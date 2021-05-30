using System;
using System.Collections.Generic;
using System.Text;
using Simplex.Protocol;
using RestSharp;
using System.Text.Json;
using RestSharp.Serializers.SystemTextJson;
using System.Threading.Tasks;
using Simplex.User;
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
        AccessCredentials AccessCredentials { get; }
        AuthRequest LoggedInUser { get; }
        ISimplexTransport Transport { get; }
        SimplexDataCache ClientCache { get; }

        Task<SimplexResponse<TRsp>> SendRequest<TRsp>(SimplexRequest rq) where TRsp : class;
        Task<SimplexBatchResponse> SendRequest(SimplexBatchRequest rq);
        Task SendPing();
        Task Connect();
        Task<SimplexResponse<AuthResponse>> LoginBasicAccount(string accountId, string accountPw);
        Task<SimplexResponse<AuthResponse>> LoginOAuth(AuthType authType);
    }

    public class SimplexClient<TTransport> : ISimplexClient where TTransport : ISimplexTransport
    {
        public SimplexClientState CLIENT_STATE { get; private set; }

        public SimplexClientConfig Config { get; }
        public SimplexServiceConfig ServiceConfig { get; private set; }
        
        public AccessCredentials AccessCredentials { get; private set; }
        public AuthRequest LoggedInUser { get; private set; }

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

        private AuthServiceParams LoginFlowAndGetAuthParams(AuthType authType)
        {
            if (CLIENT_STATE < SimplexClientState.Connected)
                throw new InvalidOperationException("Must call Connect() first.");

            if (CLIENT_STATE > SimplexClientState.Connected)
                throw new InvalidOperationException("Client is already logging in or has logged in");

            var authParams = ServiceConfig.GetAuthParams(authType);
            if (authParams == null)
                throw new InvalidOperationException($"Auth params for {authType} were not found in service config!");

            if (!authParams.Enabled)
                throw new InvalidOperationException($"Auth type {authType} is not enabled on the service!");

            CLIENT_STATE = SimplexClientState.LoggingIn;
            return authParams;
        }

        private Task<SimplexResponse<AuthResponse>> LoginBase(AuthType authType, string id, string secret)
        {
            var authParams = LoginFlowAndGetAuthParams(authType);

            logger.Info($"SimplexClient - logging into {authType} account");

            return Task.Run
                (async () =>
                {
                    if (!SimplexUtil.EncryptString(ServiceConfig.RSA, secret, out string encSecret, out var encryptErr))
                    {
                        logger.Error($"{encryptErr.Code} - {encryptErr.Message}");
                        CLIENT_STATE = SimplexClientState.Connected;
                        return null;
                    }

                    AuthRequest rq = new AuthRequest()
                    {
                        AccountID = id,
                        AccountSecret = encSecret,
                        AuthType = authType
                    };

                    try
                    {
                        var rsp = await Routines.AuthAccount(this, rq);

                        if (rsp.Error)
                        {
                            AccessCredentials = rsp.Data.Credentials;
                            LoggedInUser = rq;
                            CLIENT_STATE = SimplexClientState.LoggedIn;
                            logger.Info("SimplexClient - Logged in successfully!");
                        }
                        else
                        {
                            CLIENT_STATE = SimplexClientState.Connected;
                            logger.Error($"Could not log in! {rsp.Error}");
                        }

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

        public Task<SimplexResponse<AuthResponse>> LoginBasicAccount(string accountId, string accountPassword)
        {
            var hashedPassword = _sha.ComputeHash(Encoding.UTF8.GetBytes(accountPassword)).AsSpan().ToHexString();
            return LoginBase(AuthType.Basic, accountId, hashedPassword);
        }

        public Task<SimplexResponse<AuthResponse>> LoginOAuth(AuthType authType)
        {
            if (!authType.HasFlag(AuthType.IsOAuth))
                throw new InvalidOperationException("Must be a valid OAuth type!");

            var authParams = LoginFlowAndGetAuthParams(authType);
            
            return Task.Run
                (async () =>
                {
                    var auth = await Routines.OAuthAuthenticate(this, authParams);
                    var oauthToken = await Routines.OAuthRequestToken(this, auth, authParams);
                    var rsp = await Routines.OAuthAuthAccount(this, oauthToken.Data.TokenData, authParams);
                    return rsp;
                });
        }
    }
}
