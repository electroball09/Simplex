using System;
using System.Collections.Generic;
using System.Text;
using Simplex;
using Simplex.Protocol;
using RestSharp;
using System.Text.Json;
using Simplex.OAuthFormats;

namespace SimplexLambda.RequestHandlers
{
    public class OAuthRequestHandler : RequestHandler
    {
        public override SimplexResponse HandleRequest(SimplexRequestContext context)
        {
            var diag = context.DiagInfo.BeginDiag("OAUTH_REQUEST_HANDLER");

            SimplexResponse EndRequest(SimplexResponse rsp)
            {
                context.DiagInfo.EndDiag(diag);
                return rsp;
            }

            if (!context.Request.PayloadAs<OAuthRequestTokenRequest>(out var oauthRq, out var err))
                return context.EndRequest(err, null, diag);

            return EndRequest(HandleRequestToken(oauthRq, context));
        }

        private SimplexResponse HandleRequestToken(OAuthRequestTokenRequest oauthRq, SimplexRequestContext context)
        {
            var diag = context.DiagInfo.BeginDiag("HANDLE_REQUEST_TOKEN");

            if (!oauthRq.AuthType.HasFlag(AuthType.IsOAuth))
            {
                return context.EndRequest(
                    SimplexError.GetError(SimplexErrorCode.WTF, $"not oauth type - {oauthRq.AuthType}"),
                    null, diag);
            }

            var authParams = context.LambdaConfig.GetAuthParams(oauthRq.AuthType);
            if (authParams == null)
            {
                return context.EndRequest(
                    SimplexError.GetError(SimplexErrorCode.LambdaMisconfiguration, $"Auth params for type {oauthRq.AuthType} were not found"),
                    null, diag);
            }

            try
            {
                var type = OAuthTokenResponseData.ResponseDataMap.TypeByKey(authParams.OAuthTokenResponseDataID);

                RestRequest rq = new RestRequest(authParams.CreateTokenRequestURL(oauthRq.AuthenticationData.AuthCode, oauthRq.AuthenticationData.RedirectURI));
                rq.Method = Method.POST;
                if (!context.RestClient.EZSendRequest(rq, context.DiagInfo, out var rsp, out var restErr))
                    return context.EndRequest(restErr, null, diag);
                context.Log.Debug(rsp.ErrorMessage);
                var rspData = (OAuthTokenResponseData)JsonSerializer.Deserialize(rsp.Content, type);
                rspData.UTCTimeRequested = DateTime.UtcNow - TimeSpan.FromSeconds(10); // just to be safe

                if (rspData.error != null)
                    return context.EndRequest(SimplexError.GetError(SimplexErrorCode.Unknown, rsp.Content), null, diag);

                OAuthRequestTokenResponse response = new OAuthRequestTokenResponse()
                {
                    TokenData = rspData,
                };

                return context.EndRequest(SimplexError.OK, response, diag);
            }
            catch (Exception ex)
            {
                return context.EndRequest(SimplexError.GetError(SimplexErrorCode.Unknown, ex.ToString()), null, diag);
            }
        }
    }
}
