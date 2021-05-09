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
            context.Log.Debug($"handling request type of {context.Request.RequestType}");

            RequestHandler handler = null;
            switch (context.Request.RequestType)
            {
                case (SimplexRequestType.GetServiceConfig):
                    handler = new ServiceConfigRequestHandler();
                    break;
                case (SimplexRequestType.Auth):
                    handler = new AuthRequestHandler();
                    break;
            }

            context.Log.Debug($"handler - {handler?.GetType()}");

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
