using Simplex;
using Simplex.Protocol;
using SimplexLambda.DBSchema;
using System;
using System.Collections.Generic;
using System.Text;
using Simplex.OAuthFormats;
using RestSharp;
using System.Text.Json;
using Simplex.Util;

namespace SimplexLambda.Auth
{
    public class OAuthProvider : AuthProvider
    {
        public override SimplexError AuthUser(AuthServiceParamsLambda authParams, SimplexRequestContext context, out AuthRequest authRq, out AuthAccount acc, out SimplexError err)
        {
            var diag = context.DiagInfo.BeginDiag("OAUTH_AUTH_USER");

            acc = null;
            authRq = null;

            SimplexError EndRequest(SimplexError error)
            {
                context.DiagInfo.EndDiag(diag);
                return error;
            }

            if (!context.Request.PayloadAs<OAuthRequestAuthAccount>(out var oauthRq, out err))
                return EndRequest(err);

            authRq = oauthRq;

            var accountID = oauthRq.TokenData.IDToken.idFields.sub;
            var email = oauthRq.TokenData.IDToken.idFields.email;

            context.Log.Debug($">>> account id {accountID}");
            context.Log.Debug($">>> email {email}");

            oauthRq.AccountID = accountID;

            if (!LoadAccount(oauthRq, authParams, context, out acc, out err))
            {
                return EndRequest(err);
            }

            acc.EmailAddress = email;

            err = SimplexErrorCode.OK;
            return EndRequest(err);
        }
    }
}
