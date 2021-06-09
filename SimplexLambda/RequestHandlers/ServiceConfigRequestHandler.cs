using Simplex;
using Simplex.Protocol;
using System;
using System.Collections.Generic;
using System.Text;

namespace SimplexLambda.RequestHandlers
{
    public class ServiceConfigRequestHandler : RequestHandler
    {
        public override bool RequiresAccessToken => false;

        public override SimplexResponse HandleRequest(SimplexRequestContext context)
        {
            return new SimplexResponse(context.Request)
            {
                Error = SimplexErrorCode.OK,
                Payload = context.LambdaConfig.ServiceConfig
            };
        }
    }
}
