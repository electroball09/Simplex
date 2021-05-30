using System;
using System.Collections.Generic;
using System.Text;
using Simplex;
using Simplex.Protocol;

namespace SimplexLambda.RequestHandlers
{
    public static class Handlers
    {
        static readonly Dictionary<SimplexRequestType, Func<RequestHandler>> _handlersMap = new Dictionary<SimplexRequestType, Func<RequestHandler>>()
        {
            { SimplexRequestType.GetServiceConfig, () => new ServiceConfigRequestHandler() },
            { SimplexRequestType.Auth, () => new AuthRequestHandler() },
            { SimplexRequestType.OAuth, () => new OAuthRequestHandler() },
            { SimplexRequestType.UserData, () => new  UserDataRequestHandler() },
        };

        public static SimplexResponse HandleRequest(SimplexRequestContext context)
        {
            var diagHandle = context.DiagInfo.BeginDiag("REQUEST_HANDLER");

            context.Log.Debug($"handling request type of {context.Request.RequestType}");

            RequestHandler handler = null;
            if (_handlersMap.ContainsKey(context.Request.RequestType))
                handler = _handlersMap[context.Request.RequestType]();

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
