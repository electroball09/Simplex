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
        static SimplexLambdaConfig LambdaConfig;
        static List<string> errorNamesErrors;

        public SimplexResponse SimplexRequestHandler(SimplexRequest rq, ILambdaContext ct)
        {
            RequestDiagnostics diagInfo = new RequestDiagnostics();
            diagInfo.BeginDiag("REQUEST_OVERALL");

            if (LambdaConfig == null)
            {
                diagInfo.BeginDiag("CONFIG_LOAD");

                LambdaConfig = new SimplexLambdaConfig();
                LambdaConfig.LoadConfig();

                if (!LambdaConfig.VerifyConfig())
                {
                    return new SimplexResponse(rq)
                    {
                        Error = SimplexError.GetError(SimplexErrorCode.LambdaMisconfiguration)
                    };
                }

                errorNamesErrors = SimplexError.ValidateErrors();

                diagInfo.EndDiag("CONFIG_LOAD");
            }

            if (errorNamesErrors.Count > 0)
            {
                StringBuilder b = new StringBuilder("The following values are not defined in the errors map: ");
                foreach (string str in errorNamesErrors)
                    b.Append($"{str}|");
                b.Remove(b.Length - 1, 1);
                return new SimplexResponse(rq)
                {
                    Error = SimplexError.GetError(SimplexErrorCode.LambdaMisconfiguration, b.ToString())
                };
            }

            if (rq.RequestType == SimplexRequestType.PingPong)
            {
                diagInfo.EndDiag("REQUEST_OVERALL");
                return new SimplexResponse(rq) { Error = SimplexError.OK, DiagInfo = diagInfo };
            }

            DBWrapper db = new DBWrapper(LambdaConfig);

            SimplexRequestContext context = new SimplexRequestContext()
            {
                Request = rq,
                DB = db,
                Log = new ConsoleLogger(),
                LambdaConfig = LambdaConfig,
                DiagInfo = diagInfo,
            };

            SimplexResponse rsp;

            try
            {
                rsp = Handlers.HandleRequest(context);
            }
            catch (Exception ex)
            {
                context.Log.Error(ex);
                rsp = new SimplexResponse(rq) { Error = SimplexError.GetError(SimplexErrorCode.Unknown), Payload = LambdaConfig.DetailedErrors ? ex.ToString() : ex.Message };
            }

            if (rsp.Error == null)
            {
                rsp = new SimplexResponse(rq) { Error = SimplexError.GetError(SimplexErrorCode.ResponseMalformed, "Returned error was null!") };
            }

            if (LambdaConfig.IncludeDiagnosticInfo)
                rsp.DiagInfo = diagInfo;

            diagInfo.EndDiag("REQUEST_OVERALL");

            return rsp;
        }
    }
}
