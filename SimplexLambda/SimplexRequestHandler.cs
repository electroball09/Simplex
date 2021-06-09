using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Amazon.Lambda;
using Amazon.Lambda.Core;
using Amazon.Lambda.Serialization.SystemTextJson;
using Simplex;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using System.Text.Json;
using SimplexLambda.RequestHandlers;
using Amazon;
using Simplex.Protocol;
using System.Text;
using System.Security.Cryptography;
using Simplex.Util;
using SimplexLambda.User;

[assembly: LambdaSerializer(typeof(DefaultLambdaJsonSerializer))]
namespace SimplexLambda
{
    public class SimplexRequestContext
    {
        public SimplexRequest Request { get; set; }
        public DBWrapper DB { get; set; }
        public ISimplexLogger Log { get; set; }
        public SimplexAccessToken Token { get; set; }
        public SimplexLambdaConfig LambdaConfig { get; set; }
        public SimplexDiagnostics DiagInfo { get; set; }
        public RSACryptoServiceProvider RSA => LambdaConfig?.PrivateRSA;
        public Aes AES => LambdaConfig?.AES;
        public SHA256 SHA { get; } = SHA256.Create();
        public RestClientWrapper RestClient { get; } = new RestClientWrapper();

        public SimplexResponse EndRequest(SimplexError err, object payload, SimplexDiagnostics.DiagHandle diagHandle, Action action = null)
        {
            action?.Invoke();
            DiagInfo.EndDiag(diagHandle);
            if (!err)
                return new SimplexResponse(Request, err);
            else
                return new SimplexResponse(Request, err) { Payload = payload };
        }
    }

    public class SimplexLambdaFunctions
    {
        public static Func<string, string> ConfigLoadFunc { get; set; } = Environment.GetEnvironmentVariable;
        public static LambdaLogger Logger { get; set; } = new LambdaLogger(new ConsoleLogger());
        public static SimplexLambdaConfig LambdaConfig;
        static List<string> errorNamesErrors;
        public static bool CatchExceptions { get; set; } = true;

        ILambdaContext context;

        public SimplexResponse SimplexRequestHandler(SimplexRequest rq, ILambdaContext ct)
        {
            context = ct;
            return SimplexHandler(rq);
        }

        //public SimplexResponse SimplexAdminRequestHandler(SimplexRequest rq, ILambdaContext ct)
        //{

        //}

        public SimplexResponse SimplexHandler(SimplexRequest rq)
        {
            Logger.logs.Clear();

            Logger.Debug("handling request");

            SimplexDiagnostics diagInfo = new SimplexDiagnostics();
            var diagHandle = diagInfo.BeginDiag("REQUEST_OVERALL");

            SimplexResponse EndRequest(SimplexResponse rsp, bool overrideIncludeDiag = false)
            {
                if (rsp.Error == null)
                    return new SimplexResponse(rq, SimplexError.Custom(SimplexErrorCode.LambdaMisconfiguration, "Error from handlers was null!"));
                diagInfo.EndDiag(diagHandle);
                if (overrideIncludeDiag || LambdaConfig.IncludeDiagnosticInfo)
                    rsp.DiagInfo = diagInfo;
                if (true) //TODO: add environment var to control including logs with response
                    rsp.Logs = Logger.logs;
                return rsp;
            }

            SimplexError err;

            if (!LoadOrVerifyConfig(diagInfo, out err))
                return EndRequest(new SimplexResponse(rq, err), true);

            Logger.Debug($"config validated - {err.Code}");

            if (!VerifyErrorNames(diagInfo, out err))
                return EndRequest(new SimplexResponse(rq, err));

            Logger.Debug($"errors validated - {err.Code}");

            if (rq.RequestType == SimplexRequestType.PingPong)
                return EndRequest(new SimplexResponse(rq, SimplexErrorCode.OK));

            Logger.Debug("not ping pong");

            return EndRequest(HandleRequest(rq, diagInfo));
        }

        public SimplexResponse HandleRequest(SimplexRequest rq, SimplexDiagnostics diagInfo)
        {
            var diag = diagInfo.BeginDiag("HANDLE_REQUEST");

            SimplexRequestContext context = new SimplexRequestContext()
            {
                Request = rq,
                DB = new DBWrapper(LambdaConfig),
                Log = Logger,
                LambdaConfig = LambdaConfig,
                DiagInfo = diagInfo,
            };

            if (!Handlers.GetHandler(context, out var handler, out var err))
                return context.EndRequest(err, null, diag);

            Logger.Debug($"Handler type: {handler.GetType().Name}");
            
            if (handler.RequiresAccessToken)
            {
                Logger.Debug("this request requires access token");

                if (!SimplexAccessToken.FromString(context.Request.AccessToken, context, out var accessToken, out var tokenErr))
                    return context.EndRequest(tokenErr, null, diag);

                context.Token = accessToken;
            }

            SimplexResponse rsp;
            if (CatchExceptions)
            {
                try
                {
                    rsp = handler.HandleRequest(context);
                }
                catch (Exception ex)
                {
                    context.Log.Error(ex);
                    return new SimplexResponse(rq, SimplexError.Custom(SimplexErrorCode.Unknown, LambdaConfig.DetailedErrors ? ex.ToString() : ex.Message));
                }
            }
            else
            {
                rsp = handler.HandleRequest(context);
            }
            return rsp;
        }

        public SimplexError LoadOrVerifyConfig(SimplexDiagnostics diagInfo, out SimplexError e)
        {
            var diagHandle = diagInfo.BeginDiag("LOAD_OR_VERIFY_CONFIG");

            if (LambdaConfig == null)
            {

                LambdaConfig = SimplexLambdaConfig.Load(ConfigLoadFunc, diagInfo);

                e = LambdaConfig.ValidateConfig();
            }
            else
            {
                LambdaConfig.UpdateRollingConfig(ConfigLoadFunc, diagInfo);
                e = LambdaConfig.ValidateConfig();
            }

            diagInfo.EndDiag(diagHandle);

            return e;
        }

        public SimplexError VerifyErrorNames(SimplexDiagnostics diagInfo, out SimplexError e)
        {
            var diagHandle = diagInfo.BeginDiag("VERIFY_ERROR_NAMES");

            errorNamesErrors = SimplexError.ValidateErrors();
            if (errorNamesErrors.Count > 0)
            {
                StringBuilder b = new StringBuilder("The following values are not defined in the errors map: ");
                foreach (string str in errorNamesErrors)
                    b.Append($"{str}|");
                b.Remove(b.Length - 1, 1);
                e = SimplexError.Custom(SimplexErrorCode.LambdaMisconfiguration, b.ToString());
            }
            else
                e = SimplexErrorCode.OK;

            diagInfo.EndDiag(diagHandle);

            return e;
        }
    }
}
