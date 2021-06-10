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

        public override SimplexResult HandleRequest(SimplexRequestContext context)
        {
            return SimplexResult.OK(context.LambdaConfig.ServiceConfig);
        }
    }
}
