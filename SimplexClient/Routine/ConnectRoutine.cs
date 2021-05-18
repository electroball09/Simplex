using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Simplex;
using Simplex.Protocol;

namespace Simplex.Routine
{
    internal partial class Routines
    {
        public static async Task<SimplexResponse<SimplexServiceConfig>> ConnectRoutine(SimplexClient client)
        {
            SimplexRequest rq = new SimplexRequest(SimplexRequestType.GetServiceConfig, null);
            var cfg = await client.SendRequest<SimplexServiceConfig>(rq);
            if (!cfg.Error)
                cfg.Error.Throw();
            return cfg;
        }

        public static async Task<SimplexResponse<AccessCredentials>> AuthAccount(SimplexClient client, AuthRequest authRq)
        {
            SimplexRequest rq = new SimplexRequest(SimplexRequestType.Auth, authRq);
            Console.WriteLine("sending auth request");
            var rsp = await client.SendRequest<AccessCredentials>(rq);
            Console.WriteLine("received auth response");
            if (!rsp.Error)
            {
                if (rsp.Error == SimplexError.GetError(SimplexErrorCode.AuthAccountNonexistent))
                    client.Config.Logger.Warn("Authentication attempt failed!");
                else if (rsp.Error.Code == SimplexErrorCode.InvalidAuthCredentials)
                    client.Config.Logger.Error(rsp.Error);
                else
                    rsp.Error.Throw();
            }
            return rsp;
        }


    }
}
