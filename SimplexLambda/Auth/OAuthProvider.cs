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
        public override SimplexError AuthUser(AuthServiceParamsLambda authParams, SimplexRequestContext context, out AuthAccount acc, out SimplexError err)
        {
            var diag = context.DiagInfo.BeginDiag("OAUTH_AUTH_USER");

            acc = null;

            SimplexError EndRequest(SimplexError error)
            {
                context.DiagInfo.EndDiag(diag);
                return error;
            }

            if (!context.Request.PayloadAs<OAuthRequestAuthAccount>(out var oauthRq, out err))
                return EndRequest(err);

            RestRequest restRq = new RestRequest(authParams.OAuthAccountDataRequestURL);
            restRq.Method = Method.GET;
            restRq.AddHeader($"Authorization", $"Bearer {oauthRq.TokenData.access_token}");
            restRq.AddHeader("Client-Id", authParams.OAuthClientID);
            foreach (var kvp in authParams.OAuthAdditionalQueryParameters)
                restRq.AddQueryParameter(kvp.Key, kvp.Value);
            if (!context.RestClient.EZSendRequest(restRq, context.DiagInfo, out var rsp, out err))
                return EndRequest(err);
            context.Log.Debug(rsp.Content);

            JsonDocument doc = JsonDocument.Parse(rsp.Content);

            if (doc.GetError(out var errStr))
                return EndRequest(SimplexError.GetError(SimplexErrorCode.Unknown, errStr));

            var accountID = doc.RootElement.EvaluateString(authParams.OAuthAccountDataAccountIDLocator);
            var email = doc.RootElement.EvaluateString(authParams.OAuthAccountDataEmailAddressLocator);

            oauthRq.AccountID = accountID.GetString();

            if (!LoadAccount(oauthRq, authParams, context, out acc, out err))
            {
                return EndRequest(err);
            }

            err = SimplexError.OK;
            return EndRequest(err);
        }
    }
}
