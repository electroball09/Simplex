using System;
using System.Collections.Generic;
using System.Text;
using Simplex;
using Simplex.Protocol;
using RestSharp;
using System.Text.Json;
using Simplex.OAuthFormats;
using Flurl;

namespace SimplexLambda.RequestHandlers
{
    public class OAuthRequestHandler : RequestHandler
    {
        public override bool RequiresAccessToken => false;

        public override SimplexResponse HandleRequest(SimplexRequestContext context)
        {
            var diag = context.DiagInfo.BeginDiag("OAUTH_REQUEST_HANDLER");

            SimplexResponse EndRequest(SimplexResponse rsp)
            {
                context.DiagInfo.EndDiag(diag);
                return rsp;
            }

            if (!context.Request.PayloadAs<OAuthRequest>(out var oauthRq, out var err))
                return context.EndRequest(err, null, diag);

            if (oauthRq.RequestType == OAuthRequestType.RequestToken)
                return EndRequest(HandleRequestToken(context));

            if (oauthRq.RequestType == OAuthRequestType.RefreshToken)
                return EndRequest(HandleRefreshToken(context));

            return context.EndRequest(SimplexErrorCode.Unknown, null, diag);
        }

        private SimplexResponse HandleRequestToken(SimplexRequestContext context)
        {
            var diag = context.DiagInfo.BeginDiag("HANDLE_REQUEST_TOKEN");

            if (!context.Request.PayloadAs<OAuthRequestTokenRequest>(out var oauthRq, out var err))
                return context.EndRequest(err, null, diag);

            if (!oauthRq.ServiceIdentifier.IsOAuth)
            {
                return context.EndRequest(
                    SimplexError.Custom(SimplexErrorCode.WTF, $"not oauth type - {oauthRq.ServiceIdentifier}"),
                    null, diag);
            }

            var authParams = context.LambdaConfig.GetAuthParamsFromIdentifier(oauthRq.ServiceIdentifier);

            if (!authParams.Enabled)
                return context.EndRequest(SimplexErrorCode.AuthServiceDisabled, null, diag);

            try
            {
                var type = OAuthTokenResponseData.ResponseDataMap.TypeByKey(authParams.OAuthTokenResponseDataTypeID);

                string url = authParams.CreateTokenRequestURL(oauthRq.AuthenticationData.AuthCode, oauthRq.AuthenticationData.RedirectURI);
                if (!context.RestClient.EZPost(url, null, context.DiagInfo, out var rsp, out var restErr))
                    return context.EndRequest(restErr, null, diag);

                var rspData = (OAuthTokenResponseData)JsonSerializer.Deserialize(rsp.Content, type);
                rspData.UTCTimeRequested = DateTime.UtcNow - TimeSpan.FromSeconds(10); // just to be safe

                context.Log.Debug(rsp.Content);

                if (rspData.error != null)
                    return context.EndRequest(SimplexError.Custom(SimplexErrorCode.Unknown, rsp.Content), null, diag);

                OAuthRequestTokenResponse response = new OAuthRequestTokenResponse()
                {
                    TokenData = rspData,
                };

                return context.EndRequest(SimplexErrorCode.OK, response, diag);
            }
            catch (Exception ex)
            {
                return context.EndRequest(SimplexError.Custom(SimplexErrorCode.Unknown, ex.ToString()), null, diag);
            }
        }

        private SimplexResponse HandleRefreshToken(SimplexRequestContext context)
        {
            var diag = context.DiagInfo.BeginDiag("HANDLE_REFRESH_TOKEN");

            if (!context.Request.PayloadAs<OAuthRefreshTokenRequest>(out var oauthRq, out var err))
                return context.EndRequest(err, null, diag);

            if (!oauthRq.ServiceIdentifier.IsOAuth)
            {
                return context.EndRequest(
                    SimplexError.Custom(SimplexErrorCode.WTF, $"not oauth type - {oauthRq.ServiceIdentifier}"),
                    null, diag);
            }

            var authParams = context.LambdaConfig.GetAuthParamsFromIdentifier(oauthRq.ServiceIdentifier);

            if (!authParams.Enabled)
                return context.EndRequest(SimplexErrorCode.AuthServiceDisabled, null, diag);

            try
            {
                var type = OAuthTokenResponseData.ResponseDataMap.TypeByKey(authParams.OAuthTokenResponseDataTypeID);

                string url = authParams.CreateTokenRefreshURL(oauthRq.Token.refresh_token);
                if (!context.RestClient.EZPost(url, null, context.DiagInfo, out var rsp, out var restErr))
                    return context.EndRequest(restErr, null, diag);

                var rspData = (OAuthTokenResponseData)JsonSerializer.Deserialize(rsp.Content, type);
                rspData.UTCTimeRequested = DateTime.UtcNow - TimeSpan.FromSeconds(10); // just to be safe

                context.Log.Debug(rsp.Content);

                if (rspData.error != null)
                    return context.EndRequest(SimplexError.Custom(SimplexErrorCode.Unknown, rsp.Content), null, diag);

                rspData.refresh_token = oauthRq.Token.refresh_token; // some services don't return the refresh token so we copy it over

                OAuthRequestTokenResponse response = new OAuthRequestTokenResponse()
                {
                    TokenData = rspData,
                };

                return context.EndRequest(SimplexErrorCode.OK, response, diag);
            }
            catch (Exception ex)
            {
                return context.EndRequest(SimplexError.Custom(SimplexErrorCode.Unknown, ex.ToString()), null, diag);
            }
        }
    }
}
