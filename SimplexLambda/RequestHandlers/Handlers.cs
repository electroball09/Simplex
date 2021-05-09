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
            var diagHandle = context.DiagInfo.BeginDiag("REQUEST_HANDLER");

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

            SimplexResponse rsp;
            if (handler == null)
                rsp = new SimplexResponse(context.Request, SimplexError.GetError(SimplexErrorCode.InvalidRequestType));
            else
                rsp = handler.HandleRequest(context);

            context.DiagInfo.EndDiag(diagHandle);

            return rsp;
        }
    }

    public abstract class RequestHandler
    {
        public abstract SimplexResponse HandleRequest(SimplexRequestContext context);
    }
}
