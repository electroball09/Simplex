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
        public override bool RequiresAccessToken => false;

        public override SimplexResult HandleRequest(SimplexRequestContext context)
        {
            var diagHandle = context.DiagInfo.BeginDiag("AUTH_REQUEST_HANDLER");

            if (!context.Request.PayloadAs<AuthRequest>(out var authRq, out var err))
            {
                return context.EndRequest(SimplexResult.Err(err), diagHandle);
            }

            var authParams = context.LambdaConfig.GetAuthParamsFromIdentifier(authRq.ServiceIdentifier);

            if (!AuthProvider.GetProvider(authRq.ServiceIdentifier, out var provider, out err))
                return context.EndRequest(SimplexResult.Err(err), diagHandle);

            if (!authParams.Enabled)
                return context.EndRequest(SimplexResult.Err(SimplexErrorCode.AuthServiceDisabled), diagHandle);

            provider.AuthUser(authParams, context, out authRq, out var acc, out var authError);

            context.Log.Info($"Auth result: {authError}");
            context.Log.Debug($"create new account param: {authRq.CreateAccountIfNonexistent}");

            if (authError.Code == SimplexErrorCode.AuthAccountNonexistent)
            {
                if (!authRq.CreateAccountIfNonexistent)
                    return context.EndRequest(SimplexResult.Err(authError), diagHandle);
                else
                {
                    var newAcc = AuthAccount.Create(authParams.Identifier, authRq.AccountID);

                    if (!LambdaUtil.CreateNewAccount(ref newAcc, context, out var createErr))
                        return context.EndRequest(SimplexResult.Err(createErr), diagHandle);

                    if (!LambdaUtil.CreateNewUser(newAcc, context, out var newGuid, out var createUserErr))
                        return context.EndRequest(SimplexResult.Err(createUserErr), diagHandle);

                    if (!LambdaUtil.LinkAccountToUser(newAcc, newGuid, context, out var linkErr))
                        return context.EndRequest(SimplexResult.Err(linkErr), diagHandle);

                    newAcc.ConnectedUserGUID = newGuid;
                    newAcc.EmailAddress = acc.EmailAddress;

                    acc = newAcc;
                }
            }
            else if (!authError)
                return context.EndRequest(SimplexResult.Err(authError), diagHandle);

            acc.LastAccessedUTC = DateTime.UtcNow;
            context.DB.SaveItem(acc, out _);

            DateTime expiry = DateTime.UtcNow + TimeSpan.FromHours(context.LambdaConfig.TokenExpirationHours);
            if (authRq.OverrideAuthExpiryUTC != DateTime.MinValue)
                expiry = authRq.OverrideAuthExpiryUTC;

            SimplexAccessToken sat = new SimplexAccessToken()
            {
                UserGUID = acc.ConnectedUserGUID,
                CreatedUTC = DateTime.UtcNow,
                ExpiresUTC = expiry,
                Permissions = context.LambdaConfig.DefaultUserPermissions,
                ClientID = context.Request.ClientID,
                AuthAccountID = acc.AccountID,
                ServiceIdentifier = authParams.Identifier,
            };

            var b = sat.SerializeSignAndEncrypt(context.RSA, context.AES, context.DiagInfo);
            string tok = b.AsSpan().ToHexString();

            AccessCredentials cred = new AccessCredentials()
            {
                UserGUID = acc.ConnectedUserGUID,
                AuthToken = tok,
            };

            AuthAccountDetails accDetails = acc.ToAccountDetails(context.LambdaConfig);

            AuthResponse response = new AuthResponse()
            {
                Credentials = cred,
                AccountDetails = accDetails,
            };

            return context.EndRequest(SimplexResult.OK(response), diagHandle);
        }
    }
}
