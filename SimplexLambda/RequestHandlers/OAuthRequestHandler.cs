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

        public override SimplexResult HandleRequest(SimplexRequestContext context)
        {
            var diag = context.DiagInfo.BeginDiag("OAUTH_REQUEST_HANDLER");

            if (!context.Request.PayloadAs<OAuthRequest>(out var oauthRq, out var err))
                return context.EndRequest(SimplexResult.Err(err), diag);

            if (oauthRq.RequestType == OAuthRequestType.RequestToken)
                return context.EndRequest(HandleRequestToken(context), diag);

            if (oauthRq.RequestType == OAuthRequestType.RefreshToken)
                return context.EndRequest(HandleRefreshToken(context), diag);

            return context.EndRequest(SimplexResult.Err(SimplexErrorCode.WTF), diag);
        }

        private SimplexResult HandleRequestToken(SimplexRequestContext context)
        {
            var diag = context.DiagInfo.BeginDiag("HANDLE_REQUEST_TOKEN");

            if (!context.Request.PayloadAs<OAuthRequestTokenRequest>(out var oauthRq, out var err))
                return context.EndRequest(SimplexResult.Err(err), diag);


            if (!oauthRq.ServiceIdentifier.IsOAuth)
            {
                return context.EndRequest(SimplexResult.Err(SimplexError.Custom(SimplexErrorCode.WTF, $"not oauth type - {oauthRq.ServiceIdentifier}")), diag);
            }

            var authParams = context.LambdaConfig.GetAuthParamsFromIdentifier(oauthRq.ServiceIdentifier);

            if (!authParams.Enabled)
                return context.EndRequest(SimplexResult.Err(SimplexErrorCode.AuthServiceDisabled), diag);

            try
            {
                var type = OAuthTokenResponseData.ResponseDataMap.TypeByKey(authParams.OAuthTokenResponseDataTypeID);

                string url = authParams.CreateTokenRequestURL(oauthRq.AuthenticationData.AuthCode, oauthRq.AuthenticationData.RedirectURI);
                if (!context.RestClient.EZPost(url, null, context.DiagInfo, out var rsp, out var restErr))
                    return context.EndRequest(SimplexResult.Err(restErr), diag);

                var rspData = (OAuthTokenResponseData)JsonSerializer.Deserialize(rsp.Content, type);
                rspData.UTCTimeRequested = DateTime.UtcNow - TimeSpan.FromSeconds(10); // just to be safe

                context.Log.Debug(rsp.Content);

                if (rspData.error != null)
                    return context.EndRequest(SimplexResult.Err(SimplexError.Custom(SimplexErrorCode.Unknown, rsp.Content)), diag);

                OAuthTokenResponse response = new OAuthTokenResponse()
                {
                    TokenData = rspData,
                };

                return context.EndRequest(SimplexResult.OK(response), diag);
            }
            catch (Exception ex)
            {
                return context.EndRequest(SimplexResult.Err(SimplexError.Custom(SimplexErrorCode.Unknown, ex.ToString())), diag);
            }
        }

        private SimplexResult HandleRefreshToken(SimplexRequestContext context)
        {
            var diag = context.DiagInfo.BeginDiag("HANDLE_REFRESH_TOKEN");

            if (!context.Request.PayloadAs<OAuthRefreshTokenRequest>(out var oauthRq, out var err))
                return context.EndRequest(SimplexResult.Err(err), diag);

            if (!oauthRq.ServiceIdentifier.IsOAuth)
            {
                return context.EndRequest(SimplexResult.Err(SimplexError.Custom(SimplexErrorCode.WTF, $"not oauth type - {oauthRq.ServiceIdentifier}")), diag);
            }

            var authParams = context.LambdaConfig.GetAuthParamsFromIdentifier(oauthRq.ServiceIdentifier);

            if (!authParams.Enabled)
                return context.EndRequest(SimplexResult.Err(SimplexErrorCode.AuthServiceDisabled), diag);

            try
            {
                var type = OAuthTokenResponseData.ResponseDataMap.TypeByKey(authParams.OAuthTokenResponseDataTypeID);

                string url = authParams.CreateTokenRefreshURL(oauthRq.Token.refresh_token);
                if (!context.RestClient.EZPost(url, null, context.DiagInfo, out var rsp, out var restErr))
                    return context.EndRequest(SimplexResult.Err(restErr), diag);

                var rspData = (OAuthTokenResponseData)JsonSerializer.Deserialize(rsp.Content, type);
                rspData.UTCTimeRequested = DateTime.UtcNow - TimeSpan.FromSeconds(10); // just to be safe

                context.Log.Debug(rsp.Content);

                if (rspData.error != null)
                    return context.EndRequest(SimplexResult.Err(SimplexError.Custom(SimplexErrorCode.Unknown, rsp.Content)), diag);

                rspData.refresh_token = oauthRq.Token.refresh_token; // some services don't return the refresh token so we copy it over

                OAuthTokenResponse response = new OAuthTokenResponse()
                {
                    TokenData = rspData,
                };

                if (oauthRq.Token.IDToken != rspData.IDToken)
                    rspData.id_token = oauthRq.Token.id_token;

                return context.EndRequest(SimplexResult.OK(response), diag);
            }
            catch (Exception ex)
            {
                return context.EndRequest(SimplexResult.Err(SimplexError.Custom(SimplexErrorCode.Unknown, ex.ToString())), diag);
            }
        }
    }
}
