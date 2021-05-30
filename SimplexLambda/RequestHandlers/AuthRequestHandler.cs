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

            if (!context.Request.PayloadAs<AuthRequest>(out var authRq, out var err))
            {
                return context.EndRequest(
                    SimplexError.GetError(SimplexErrorCode.InvalidAuthCredentials, "Invalid payload type"),
                    null, diagHandle);
            }

            var authParams = context.LambdaConfig.GetAuthParams(authRq.AuthType);

            AuthProvider provider = AuthProvider.GetProvider(authRq.AuthType);

            if (!provider.AuthUser(authParams, context, out var acc, out var authError))
            {
                return context.EndRequest(authError, "something is fucked!", diagHandle);
            }

            SimplexAccessToken sat = new SimplexAccessToken()
            {
                UserGUID = acc.ConnectedUserGUID,
                Created = DateTime.UtcNow,
                AccessFlags = SimplexAccessFlags.GetUserData | SimplexAccessFlags.SetUserData | SimplexAccessFlags.UpdateUserData// | SimplexAccessFlags.Admin
            };

            var b = sat.SerializeSignAndEncrypt(context.RSA, context.AES, context.DiagInfo);
            string tok = b.AsSpan().ToHexString();

            AccessCredentials cred = new AccessCredentials()
            {
                UserGUID = acc.ConnectedUserGUID,
                AuthToken = tok,
            };

            AuthResponse response = new AuthResponse()
            {
                AuthType = authRq.AuthType,
                Credentials = cred,
            };

            return context.EndRequest(SimplexError.OK, "this is a response!", diagHandle);
        }
    }
}
