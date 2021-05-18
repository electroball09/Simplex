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

    public class SimplexClient
    {
        public SimplexClientState CLIENT_STATE { get; private set; }

        public SimplexClientConfig Config { get; }
        public SimplexServiceConfig ServiceConfig { get; private set; }
        
        public AccessCredentials LoggedInUser { get; private set; }
        public AuthRequest LoggedInCredentials { get; private set; }

        internal ISimplexTransport transport { get; }

        public SimplexClient(SimplexClientConfig cfg)
        {
            Config = cfg.Copy();

            transport = cfg.Transport;
            transport.Logger = cfg.Logger;
            transport.Initialize();
        }

        public Task<SimplexResponse<TRsp>> SendRequest<TRsp>(SimplexRequest rq) where TRsp : class
        {
            return transport.SendRequest<SimplexResponse<TRsp>>(rq);
        }

        public Task<SimplexBatchResponse> SendRequest(SimplexBatchRequest rq)
        {
            return transport.SendRequest<SimplexBatchResponse>(rq);
        }

        public Task SendPing()
        {
            var task = Task.Run
                (async () =>
                {
                    var rsp = await SendRequest<object>(new SimplexRequest(SimplexRequestType.PingPong, null));
                });
            return task;
        }

        public Task Connect()
        {
            if (CLIENT_STATE == SimplexClientState.Connecting)
                throw new InvalidOperationException("Trying to connect while client is waiting for server connection response.");

            if (CLIENT_STATE != SimplexClientState.Unconnected)
                throw new InvalidOperationException("Client has already connected!");

            Config.Logger.Info("SimplexClient - connecting...");

            CLIENT_STATE = SimplexClientState.Connecting;

            var task = Task.Run
                (async () =>
                {
                    try
                    {
                        var cfg = await Routines.ConnectRoutine(this);
                        ServiceConfig = cfg.Data;
                        CLIENT_STATE = SimplexClientState.Connected;
                        Config.Logger.Info("SimplexClient - connected!");
                        Config.Logger.Debug($"RSA key - {ServiceConfig.PublicKeyHex}");
                    }
                    catch (Exception ex)
                    {
                        CLIENT_STATE = SimplexClientState.Unconnected;
                        Config.Logger.Error(ex.ToString());
                    }
                });
            return task;
        }

        private Task<SimplexResponse<AccessCredentials>> LoginBase(AuthType authType, string id, string secret)
        {
            if (CLIENT_STATE < SimplexClientState.Connected)
                throw new InvalidOperationException("Must call Connect() first.");

            if (CLIENT_STATE > SimplexClientState.Connected)
                throw new InvalidOperationException("Client is already logging in or has logged in");

            CLIENT_STATE = SimplexClientState.LoggingIn;
            Config.Logger.Info($"SimplexClient - logging into {authType} account");

            return Task.Run
                (async () =>
                {
                    if (!SimplexUtil.EncryptString(ServiceConfig.RSA, secret, out string encSecret, out var encryptErr))
                    {
                        Config.Logger.Error($"{encryptErr.Code} - {encryptErr.Message}");
                        CLIENT_STATE = SimplexClientState.Connected;
                        return null;
                    }

                    Config.Logger.Debug("data encrypted");

                    AuthRequest rq = new AuthRequest()
                    {
                        AuthID = id,
                        AuthSecret = encSecret,
                        AuthType = authType
                    };

                    try
                    {
                        var rsp = await Routines.AuthAccount(this, rq);

                        if (rsp.Error)
                        {
                            LoggedInUser = rsp.Data;
                            LoggedInCredentials = rq;
                            CLIENT_STATE = SimplexClientState.LoggedIn;
                            Config.Logger.Info("SimplexClient - Logged in successfully!");
                        }
                        else
                        {
                            CLIENT_STATE = SimplexClientState.Connected;
                            Config.Logger.Error("Could not log in!");
                        }

                        return rsp;
                    }
                    catch (Exception ex)
                    {
                        CLIENT_STATE = SimplexClientState.Connected;
                        Config.Logger.Error(ex.ToString());
                        return null;
                    }
                });
        }

        public Task<SimplexResponse<AccessCredentials>> LoginBasicAccount(string accountId, string accountPassword)
        {
            return LoginBase(AuthType.Basic, accountId, accountPassword);
        }
    }
}
