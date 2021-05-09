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
            SimplexLambdaFunctions.ConfigLoadFunc = envVarFunc;
        }

        public Task<SimplexResponse<T>> SendRequest<T>(SimplexRequest rq) where T : class
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
                    Logger.Debug($"{rsp.Payload} - {rsp.PayloadType}");
                    SimplexResponse<T> rspDeserialized = JsonSerializer.Deserialize<SimplexResponse<T>>(rspJson);
                    rspDeserialized.DeserializePayload();
                    return rspDeserialized;
                });
        }
    }
}
