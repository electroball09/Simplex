using System;
using System.Collections.Generic;
using System.Text;
using Simplex;
using Simplex.Protocol;

namespace SimplexLambda.RequestHandlers
{
    public static class Handlers
    {
        public static SimplexResponse HandleRequest(SimplexRequestContext context)
        {
            RequestHandler handler = null;
            switch (context.Request.RequestType)
            {
                case (SimplexRequestType.Auth):
                    handler = new AuthRequestHandler();
                    break;
                case (SimplexRequestType.GetServiceConfig):
                    handler = new ServiceConfigRequestHandler();
                    break;
            }

            if (handler == null)
                return new SimplexResponse(context.Request) { Error = SimplexError.GetError(SimplexErrorCode.InvalidRequestType) };

            return handler.HandleRequest(context);
        }
    }

    public abstract class RequestHandler
    {
        public abstract SimplexResponse HandleRequest(SimplexRequestContext context);
    }
}
