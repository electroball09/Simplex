using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Simplex;
using Simplex.Protocol;

namespace Simplex.Routine
{
    public partial class Routines
    {
        public static async Task<SimplexResult> ConnectRoutine(ISimplexClient client)
        {
            SimplexRequest rq = new SimplexRequest(SimplexRequestType.GetServiceConfig, null);
            return (await client.SendRequest(rq)).Result;
        }

        public static async Task<SimplexResult> AuthAccount(ISimplexClient client, AuthRequest authRq)
        {
            SimplexRequest rq = new SimplexRequest(SimplexRequestType.Auth, authRq);

            client.Config.Logger.Debug("sending auth request");
            var rsp = await client.SendRequest(rq);
            client.Config.Logger.Debug("received auth response");

            return rsp.Result;
        }


    }
}
