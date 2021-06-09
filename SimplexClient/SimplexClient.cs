using System;
using System.Collections.Generic;
using System.Text;
using Simplex.Protocol;
using RestSharp;
using System.Text.Json;
using RestSharp.Serializers.SystemTextJson;
using System.Threading.Tasks;
using Simplex.UserData;
using Simplex.Routine;
using Simplex.Transport;
using Simplex.Util;
using System.Security.Cryptography;
using System.ComponentModel.DataAnnotations;

namespace Simplex
{
    public enum SimplexClientState
    {
        Unconnected,
        Connecting,
        Connected,
        LoggingIn,
        LoggedIn
    }

    public interface ISimplexClient
    {
        SimplexClientState CLIENT_STATE { get; }
        SimplexClientConfig Config { get; }
        SimplexServiceConfig ServiceConfig { get; }
        AuthResponse LoggedInUser { get; }
        ISimplexTransport Transport { get; }
        SimplexDataCache ClientCache { get; }

        Task<SimplexResponse<TRsp>> SendRequest<TRsp>(SimplexRequest rq) where TRsp : class;
        Task<SimplexBatchResponse> SendRequest(SimplexBatchRequest rq);
        Task SendPing();
        Task Connect();
        Task<SimplexResponse<AuthResponse>> LoginBasicAccount(string accountId, string accountPw);
        Task<SimplexResponse<AuthResponse>> LoginOAuth(AuthServiceIdentifier serviceIdentifier);
    }

    public class SimplexClient<TTransport> : ISimplexClient where TTransport : ISimplexTransport
    {
        public SimplexClientState CLIENT_STATE { get; private set; }

        public SimplexClientConfig Config { get; }
        public SimplexServiceConfig ServiceConfig { get; private set; }
        
        public AuthResponse LoggedInUser { get; private set; }

        public SimplexDataCache ClientCache { get; private set; }
        public SimplexDataCache UserCache { get; private set; }

        private TTransport transport;
        ISimplexTransport ISimplexClient.Transport => transport;
        private ISimplexLogger logger;

        SHA256 _sha = SHA256.Create();

        public SimplexClient(SimplexClientConfig cfg, TTransport transportInst)
        {
            Validator.ValidateObject(cfg, new ValidationContext(cfg), true);

            Config = cfg.Copy();

            Config.Logger = new PrefixedSimplexLogger(cfg.Logger, cfg.ClientID);
            logger = Config.Logger;

            transport = transportInst;
            transport.Logger = cfg.Logger;
            transport.Initialize();

            ClientCache = new SimplexDataCache(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                Config.ClientID,
                Config.GameName);
        }

        public Task<SimplexResponse<TRsp>> SendRequest<TRsp>(SimplexRequest rq) where TRsp : class
        {
            rq.ClientID = Config.ClientID;
            return transport.SendRequest<SimplexResponse<TRsp>>(rq);
        }

        public Task<SimplexBatchResponse> SendRequest(SimplexBatchRequest rq)
        {
            rq.ClientID = Config.ClientID;
            return transport.SendRequest<SimplexBatchResponse>(rq);
        }

        public Task SendPing()
        {
            var task = Task.Run
                (async () =>
                {
                    logger.Info("Sending ping...");
                    var rsp = await SendRequest<object>(new SimplexRequest(SimplexRequestType.PingPong, null));
                    logger.Info("Received pong!");
                });
            return task;
        }

        public Task Connect()
        {
            if (CLIENT_STATE > SimplexClientState.Unconnected)
                throw new InvalidOperationException($"Client is connecting or has already connected!\nCLIENT STATE: {CLIENT_STATE}");

            logger.Info("SimplexClient - connecting...");

            CLIENT_STATE = SimplexClientState.Connecting;

            var task = Task.Run
                (async () =>
                {
                    try
                    {
                        var cfg = await Routines.ConnectRoutine(this);

                        if (!cfg.Error)
                            cfg.Error.Throw();

                        ServiceConfig = cfg.Data;
                        CLIENT_STATE = SimplexClientState.Connected;
                        logger.Info("SimplexClient - connected!");
                        logger.Debug($"RSA key - {ServiceConfig.PublicKeyHex}");
                    }
                    catch (Exception ex)
                    {
                        CLIENT_STATE = SimplexClientState.Unconnected;
                        logger.Error(ex.ToString());
                    }
                });
            return task;
        }

        private AuthServiceParams LoginFlow_BeginAndGetAuthParams(AuthServiceIdentifier serviceIdentifier)
        {
            logger.Info($"Begin login flow for service {serviceIdentifier}");

            if (CLIENT_STATE < SimplexClientState.Connected)
                throw new InvalidOperationException("Must call Connect() first.");

            if (CLIENT_STATE > SimplexClientState.Connected)
                throw new InvalidOperationException("Client is already logging in or has logged in");

            var authParams = ServiceConfig.GetParamsByIdentifier(serviceIdentifier);
            if (authParams == null)
                throw new InvalidOperationException($"Auth params for {serviceIdentifier} were not found in service config!");

            if (!authParams.Enabled)
                throw new InvalidOperationException($"Auth service {serviceIdentifier} is not enabled on the service!");

            CLIENT_STATE = SimplexClientState.LoggingIn;
            return authParams;
        }

        private async Task LoginFlow_OnAuthResponseReceived(SimplexResponse<AuthResponse> rsp)
        {
            if (!rsp.Error)
            {
                logger.Error($"Login failed! {rsp.Error}");
                CLIENT_STATE = SimplexClientState.Connected;
            }
            else
            {
                logger.Info("Logged in successfully!");
                LoggedInUser = rsp.Data;
                CLIENT_STATE = SimplexClientState.LoggedIn;

                await Routines.CacheLoggedInUser(this, LoggedInUser);
            }
        }

        public Task<SimplexResponse<AuthResponse>> LoginBasicAccount(string accountId, string accountPassword)
        {
            var authParams = LoginFlow_BeginAndGetAuthParams(ServiceConfig.GetParamsByType(AuthServiceIdentifier.AuthType.Basic).Identifier);

            var hashedPassword = _sha.ComputeHash(Encoding.UTF8.GetBytes(accountPassword)).AsSpan().ToHexString();

            return Task.Run
                (async () =>
                {
                    if (!SimplexUtil.EncryptString(ServiceConfig.RSA, hashedPassword, out string encSecret, out var encryptErr))
                    {
                        logger.Error($"{encryptErr}");
                        CLIENT_STATE = SimplexClientState.Connected;
                        return null;
                    }

                    AuthRequest rq = new AuthRequest()
                    {
                        AccountID = accountId,
                        AccountSecret = encSecret,
                        ServiceIdentifier = authParams.Identifier,
                        CreateAccountIfNonexistent = Config.CreateAccountIfNonexistent
                    };

                    try
                    {
                        var rsp = await Routines.AuthAccount(this, rq);

                        await LoginFlow_OnAuthResponseReceived(rsp);

                        return rsp;
                    }
                    catch (Exception ex)
                    {
                        CLIENT_STATE = SimplexClientState.Connected;
                        logger.Error(ex.ToString());
                        return null;
                    }
                });
        }

        public Task<SimplexResponse<AuthResponse>> LoginOAuth(AuthServiceIdentifier serviceIdentifier)
        {
            if (!serviceIdentifier.IsOAuth)
                throw new InvalidOperationException("Must be a valid OAuth type!");

            var authParams = LoginFlow_BeginAndGetAuthParams(serviceIdentifier);

            return Task.Run
                (async () =>
                {
                    logger.Info($"Trying to locate cached OAuth token for {authParams.Identifier}...");
                    var tok = await Routines.OAuthTryLocateCachedToken(this, authParams.Identifier);
                    if (tok != null)
                    {
                        if (DateTime.UtcNow > tok.UTCTimeRequested + TimeSpan.FromSeconds(tok.expires_in))
                        {
                            logger.Info("Cached token has expired, refreshing token...");
                            tok = (await Routines.OAuthRefreshToken(this, tok, authParams)).Data.TokenData;
                        }
                        else
                        {
                            logger.Info("Found cached token!");
                        }
                    }
                    else
                    {
                        logger.Info("Could not locate cached token, authenticating user...");
                        var auth = await Routines.OAuthAuthenticate(this, authParams);
                        tok = (await Routines.OAuthRequestToken(this, auth, authParams)).Data.TokenData;
                    }
                    var rsp = await Routines.OAuthAuthAccount(this, tok, authParams);

                    await LoginFlow_OnAuthResponseReceived(rsp);

                    return rsp;
                });
        }
    }
}
