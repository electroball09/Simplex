using System;
using System.Collections.Generic;
using System.Text;
using Simplex;
using SimplexLambda.Auth;
using System.Text.Json;
using Simplex.Protocol;
using SimplexLambda.DBSchema;
using SimplexLambda.User;
using Simplex.Util;

namespace SimplexLambda.RequestHandlers
{
    public class AuthRequestHandler : RequestHandler
    {
        public override SimplexResponse HandleRequest(SimplexRequestContext context)
        {
            var diagHandle = context.DiagInfo.BeginDiag("AUTH_REQUEST_HANDLER");

            SimplexResponse EndRequest(SimplexError err, object payload = null)
            {
                context.DiagInfo.EndDiag(diagHandle);
                if (!err)
                    return new SimplexResponse(context.Request, err);
                else
                    return new SimplexResponse(context.Request, err) { Payload = payload };
            }

            AuthRequest authRq = context.DeserializePayload<AuthRequest>();

            if (!SimplexUtil.DecryptString(context.LambdaConfig.PrivateRSA, authRq.AuthSecret, out string decryptedSecret, out var decryptError))
            {
                return EndRequest(SimplexError.GetError(SimplexErrorCode.InvalidAuthCredentials));
            }
            authRq.AuthSecret = decryptedSecret;

            AuthAccount acc = AuthAccount.Create(authRq.AuthType, authRq.AuthID);

            if (!context.DB.LoadItem(acc, out acc, context, out var loadErr))
            {
                if (loadErr.Code == SimplexErrorCode.DBItemNonexistent)
                    loadErr = SimplexError.GetError(SimplexErrorCode.AuthAccountNonexistent);

                return EndRequest(loadErr);
            }

            if (!AuthProvider.GetProvider(authRq.AuthType).AuthUser(authRq, acc, context, out var authErr))
                return EndRequest(authErr);

            SimplexAccessToken sat = new SimplexAccessToken()
            {
                UserGUID = acc.ConnectedUserGUID,
                Created = DateTime.UtcNow,
                //AccessFlags = SimplexAccessFlags.GetUserData | SimplexAccessFlags.SetUserData | SimplexAccessFlags.UpdateUserData | SimplexAccessFlags.Admin
            };

            var b = sat.SerializeSignAndEncrypt(context.RSA, context.AES, context.DiagInfo);
            string tok = new Span<byte>(b).ToHexString();

            AccessCredentials cred = new AccessCredentials()
            {
                UserGUID = acc.ConnectedUserGUID,
                AuthToken = tok,
                
            };

            return EndRequest(SimplexError.OK, cred);
        }
    }
}
