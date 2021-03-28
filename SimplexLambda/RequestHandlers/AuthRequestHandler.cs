using System;
using System.Collections.Generic;
using System.Text;
using Simplex;
using SimplexLambda.Auth;
using System.Text.Json;
using Simplex.Protocol;
using SimplexLambda.DBSchema;

namespace SimplexLambda.RequestHandlers
{
    public class AuthRequestHandler : RequestHandler
    {
        public override SimplexResponse HandleRequest(SimplexRequestContext context)
        {
            context.DiagInfo.BeginDiag("AUTH_REQUEST_HANDLER");
            void __EndDiag() => context.DiagInfo.EndDiag("AUTH_REQUEST_HANDLER");

            AuthRequest authRq = JsonSerializer.Deserialize<AuthRequest>(((JsonElement)context.Request.Payload).GetRawText());
            var (decryptErr, str) = SimplexUtil.DecryptString(context.LambdaConfig.RSA, authRq.AuthSecret);
            authRq.AuthSecret = str;

            if (decryptErr)
            {
                __EndDiag();
                return new SimplexResponse(context.Request) { Error = SimplexError.GetError(SimplexErrorCode.InvalidAuthCredentials) };
            }

            AuthAccount acc = AuthAccount.Create(authRq.AuthType, authRq.AuthID);

            var loadErr = context.DB.LoadItem(acc, out acc, context);
            if (loadErr)
            {
                __EndDiag();

                if (loadErr.Code == SimplexErrorCode.DBItemNonexistent)
                    return new SimplexResponse(context.Request) { Error = SimplexError.GetError(SimplexErrorCode.AuthAccountNonexistent) };
                else
                    return new SimplexResponse(context.Request) { Error = loadErr };
            }

            AuthProvider provider = AuthProvider.GetProvider(authRq.AuthType);

            var authErr = provider.AuthUser(authRq, acc, context);
            if (authErr)
            {
                __EndDiag();
                return new SimplexResponse(context.Request) { Error = authErr };
            }

            var (tokenErr, token) = LambdaUtil.GenerateAccessToken(acc.ConnectedSimplexGUID, context);
            if (tokenErr)
            {
                __EndDiag();
                return new SimplexResponse(context.Request) { Error = tokenErr };
            }

            UserCredentials cred = new UserCredentials()
            {
                AuthGUID = acc.ConnectedSimplexGUID,
                AuthToken = token.Token
            };

            __EndDiag();

            return new SimplexResponse(context.Request)
            {
                Error = SimplexError.OK,
                Payload = cred,
            };
        }
    }
}
