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

        public static SimplexError GetHandler(SimplexRequestContext context, out RequestHandler handler, out SimplexError err)
        {
            handler = null;

            if (!_handlersMap.TryGetValue(context.Request.RequestType, out var handlerCreater))
                err = SimplexErrorCode.InvalidRequestType;
            else
            {
                handler = handlerCreater();
                err = SimplexErrorCode.OK;
            }

            return err;
        }
    }

    public abstract class RequestHandler
    {
        public abstract bool RequiresAccessToken { get; }
        public abstract SimplexResult HandleRequest(SimplexRequestContext context);
    }
}
