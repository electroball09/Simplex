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
        
        public UserCredentials LoggedInUser { get; private set; }
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
            return transport.SendRequest<TRsp>(rq);
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
            {
                Config.Logger.Warn("Trying to connect while client is waiting for server connection response.");
                return null;
            }

            if (CLIENT_STATE != SimplexClientState.Unconnected)
            {
                Config.Logger.Error("Client has already connected!");
                return null;
            }

            Config.Logger.Info("SimplexClient - connecting...");

            CLIENT_STATE = SimplexClientState.Connecting;

            var task = Task.Run
                (async () =>
                {
                    try
                    {
                        var cfg = await Routines.ConnectRoutine(this);
                        if (cfg.Item == null)
                        {
                            CLIENT_STATE = SimplexClientState.Unconnected;
                            Config.Logger.Error("Server returned no config.  Cannot connect.");
                        }
                        else
                        {
                            ServiceConfig = cfg.Item;
                            CLIENT_STATE = SimplexClientState.Connected;
                            Config.Logger.Info("SimplexClient - connected!");
                        }
                    }
                    catch (Exception ex)
                    {
                        CLIENT_STATE = SimplexClientState.Unconnected;
                        Config.Logger.Error(ex.ToString());
                    }
                });
            return task;
        }

        private Task<SimplexResponse<UserCredentials>> LoginBase(AuthType authType, string id, string secret)
        {
            if (CLIENT_STATE < SimplexClientState.Connected)
            {
                Config.Logger.Error("Must call Connect() first.");
                return null;
            }

            if (CLIENT_STATE > SimplexClientState.Connected)
            {
                Config.Logger.Error("Client is logging in or has logged in");
                return null;
            }

            CLIENT_STATE = SimplexClientState.LoggingIn;
            Config.Logger.Info($"SimplexClient - logging into {authType} account");

            return Task.Run
                (async () =>
                {
                    if (!SimplexUtil.EncryptData(ServiceConfig.RSA, secret, out string encSecret, out var encryptErr))
                        encryptErr.Throw();

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
                            LoggedInUser = rsp.Item;
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

        public Task<SimplexResponse<UserCredentials>> LoginBasicAccount(string accountId, string accountPassword)
        {
            return LoginBase(AuthType.Basic, accountId, accountPassword);
        }
    }
}
