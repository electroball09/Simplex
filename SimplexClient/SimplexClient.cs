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

        private RestClient client;

        public SimplexClient(SimplexClientConfig cfg)
        {
            Config = cfg.Copy();

            client = new RestClient(Config.AssembleURL());
            client.UseSerializer<SystemTextJsonSerializer>();
        }

        public SimplexClient() : this(SimplexClientConfig.Default) { }

        public async Task<SimplexResponse<TRsp>> SendRequest<TRsp>(SimplexRequest request) where TRsp : class
        {
            return await Task.Run
                (async () =>
                {
                    Config.Logger.Debug($"Sending request of type {request.RequestType}");

                    var json = await RequestGetJson(request);
                    Config.Logger.Debug($"-request ID {request.RequestID} took {(DateTime.Now - request.RequestedTime).TotalMilliseconds} ms");
                    var rsp = JsonSerializer.Deserialize<SimplexResponse<TRsp>>(json);
                    rsp.DiagInfo?.DebugLog(Config.Logger);
                    if (!rsp.Error && !rsp.DeserializePayload())
                    {
                        Config.Logger.Error($"Response type mismatch! Wanted type: {typeof(TRsp).Name}  received type: {rsp.PayloadType}");
                        rsp.Payload = null;
                        rsp.Error = SimplexError.GetError(SimplexErrorCode.InvalidResponsePayloadType);
                    }
                    return rsp;
                }, new System.Threading.CancellationToken());
        }

        internal async Task<string> RequestGetJson(SimplexRequest request)
        {
            var rest = new RestRequest("", Method.POST, DataFormat.Json);
            string jsonStr = JsonSerializer.Serialize(request);
            Config.Logger.Debug($"request - {jsonStr}");
            rest.AddJsonBody(request);
            var resp = await client.ExecuteAsync<SimplexResponse>(rest);
            if (resp == null)
                throw new InvalidOperationException("Server returned a null response!");
            Config.Logger.Debug($"response - {resp.Content}");
            return resp.Content;
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
                    string encSecret = SimplexUtil.EncryptData(ServiceConfig.RSA, secret);

                    AuthRequest rq = new AuthRequest()
                    {
                        AuthID = id,
                        AuthSecret = encSecret,
                        AuthType = authType,
                        AutoCreateAccount = Config.LoginAutoCreateAccount
                    };

                    try
                    {
                        var rsp = await Routines.AuthAccount(this, rq);

                        if (!rsp.Error)
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
