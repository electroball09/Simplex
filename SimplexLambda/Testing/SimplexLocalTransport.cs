using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Simplex;
using Simplex.Protocol;
using Simplex.Transport;
using SimplexLambda;

namespace SimplexLambda.Testing
{
    public class SimplexLocalTransport : ISimplexTransport
    {
        public ISimplexLogger Logger { get; set; }

        SimplexLambdaFunctions funcs;
        Func<string, string> envVarFunc;

        public SimplexLocalTransport()
        {

        }

        public void SetEnvVarFunc(Func<string, string> getEnvVarFunc)
        {
            envVarFunc = getEnvVarFunc;
        }

        public void Initialize()
        {
            funcs = new SimplexLambdaFunctions();
            SimplexLambdaFunctions.Logger = new LambdaLogger(Logger);
            SimplexLambdaFunctions.ConfigLoadFunc = envVarFunc;
            SimplexLambdaFunctions.CatchExceptions = false;
        }

        public Task<SimplexResponse> SendRequest(SimplexRequest rq)
        {
            return Task.Run
                (() =>
                {
                    string rqJson = JsonSerializer.Serialize(rq);
                    Logger.Debug(rqJson);
                    SimplexRequest rqDeserialized = JsonSerializer.Deserialize<SimplexRequest>(rqJson);
                    SimplexResponse rsp = funcs.SimplexHandler(rqDeserialized);
                    string rspJson = JsonSerializer.Serialize(rsp);
                    Logger.Debug(rspJson);
                    rsp.DiagInfo.DebugLog(Logger);
                    SimplexResponse rspDeserialized = JsonSerializer.Deserialize<SimplexResponse>(rspJson);
                    rspDeserialized.Logger = Logger;
                    return rspDeserialized;
                });
        }
    }
}
