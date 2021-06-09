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
        public static async Task<SimplexResponse<SimplexServiceConfig>> ConnectRoutine(ISimplexClient client)
        {
            SimplexRequest rq = new SimplexRequest(SimplexRequestType.GetServiceConfig, null);
            var cfg = await client.SendRequest<SimplexServiceConfig>(rq);
            return cfg;
        }

        public static async Task<SimplexResponse<AuthResponse>> AuthAccount(ISimplexClient client, AuthRequest authRq)
        {
            SimplexRequest rq = new SimplexRequest(SimplexRequestType.Auth, authRq);
            client.Config.Logger.Debug("sending auth request");
            var rsp = await client.SendRequest<AuthResponse>(rq);
            client.Config.Logger.Debug("received auth response");
            if (!rsp.Error)
            {
                if (rsp.Error == SimplexErrorCode.AuthAccountNonexistent)
                    client.Config.Logger.Warn("Authentication attempt failed!");
                else if (rsp.Error.Code == SimplexErrorCode.InvalidAuthCredentials)
                    client.Config.Logger.Error(rsp.Error);
            }
            return rsp;
        }


    }
}
