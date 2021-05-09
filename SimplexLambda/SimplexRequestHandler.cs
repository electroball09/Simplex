using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
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

[assembly: LambdaSerializer(typeof(DefaultLambdaJsonSerializer))]
namespace SimplexLambda
{
    public class SimplexRequestContext
    {
        public SimplexRequest Request { get; set; }
        public DBWrapper DB { get; set; }
        public ISimplexLogger Log { get; set; }
        public SimplexLambdaConfig LambdaConfig { get; set; }
        public RequestDiagnostics DiagInfo { get; set; }
    }

    public class SimplexLambdaFunctions
    {
        public static Func<string, string> ConfigLoadFunc { get; set; } = Environment.GetEnvironmentVariable;
        public static ISimplexLogger Logger { get; set; } = new ConsoleLogger();
        static SimplexLambdaConfig LambdaConfig;
        static List<string> errorNamesErrors;

        ILambdaContext context;

        public SimplexResponse SimplexRequestHandler(SimplexRequest rq, ILambdaContext ct)
        {
            context = ct;
            return SimplexHandler(rq);
        }

        public SimplexResponse SimplexHandler(SimplexRequest rq)
        {
            Logger.Debug("handling request");

            RequestDiagnostics diagInfo = new RequestDiagnostics();
            diagInfo.BeginDiag("REQUEST_OVERALL");

            SimplexResponse EndRequest(SimplexResponse rsp, bool overrideIncludeDiag = false)
            {
                if (rsp.Error == null)
                    return new SimplexResponse(rq, SimplexError.GetError(SimplexErrorCode.LambdaMisconfiguration, "Error from handlers was null!"));
                diagInfo.EndDiag("REQUEST_OVERALL");
                if (overrideIncludeDiag || LambdaConfig.IncludeDiagnosticInfo)
                    rsp.DiagInfo = diagInfo;
                return rsp;
            }

            SimplexError err;

            if (LoadOrVerifyConfig(diagInfo, out err))
                return EndRequest(new SimplexResponse(rq, err), true);

            Logger.Debug($"config validated - {err.Code}");

            if (VerifyErrorNames(diagInfo, out err))
                return EndRequest(new SimplexResponse(rq, err));

            Logger.Debug($"errors validated - {err.Code}");

            if (rq.RequestType == SimplexRequestType.PingPong)
                return EndRequest(new SimplexResponse(rq, SimplexError.OK));

            Logger.Debug("not ping pong");

            return EndRequest(HandleRequest(rq, diagInfo));
        }

        public SimplexResponse HandleRequest(SimplexRequest rq, RequestDiagnostics diagInfo)
        {
            DBWrapper db = new DBWrapper(LambdaConfig);

            SimplexRequestContext context = new SimplexRequestContext()
            {
                Request = rq,
                DB = db,
                Log = Logger,
                LambdaConfig = LambdaConfig,
                DiagInfo = diagInfo,
            };

            try
            {
                var rsp = Handlers.HandleRequest(context);
                return rsp;
            }
            catch (Exception ex)
            {
                context.Log.Error(ex);
                return new SimplexResponse(rq, SimplexError.GetError(SimplexErrorCode.Unknown, LambdaConfig.DetailedErrors ? ex.ToString() : ex.Message));
            }
        }

        public SimplexError LoadOrVerifyConfig(RequestDiagnostics diagInfo, out SimplexError e)
        {
            if (LambdaConfig == null)
            {
                diagInfo.BeginDiag("CONFIG_LOAD");

                LambdaConfig = new SimplexLambdaConfig();
                LambdaConfig.Load(ConfigLoadFunc);

                e = LambdaConfig.ValidateConfig();

                if (e)
                    LambdaConfig = null;

                diagInfo.EndDiag("CONFIG_LOAD");
            }
            else
                e = SimplexError.OK;

            return e;
        }

        public SimplexError VerifyErrorNames(RequestDiagnostics diagInfo, out SimplexError e)
        {
            errorNamesErrors = SimplexError.ValidateErrors();
            if (errorNamesErrors.Count > 0)
            {
                StringBuilder b = new StringBuilder("The following values are not defined in the errors map: ");
                foreach (string str in errorNamesErrors)
                    b.Append($"{str}|");
                b.Remove(b.Length - 1, 1);
                e = SimplexError.GetError(SimplexErrorCode.LambdaMisconfiguration, b.ToString());
            }
            else
                e = SimplexError.OK;

            return e;
        }
    }
}
