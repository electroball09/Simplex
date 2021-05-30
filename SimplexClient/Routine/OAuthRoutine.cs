using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using Simplex;
using Simplex.Protocol;
using Simplex.Util;
using Simplex.OAuthFormats;

namespace Simplex.Routine
{
    internal partial class Routines
    {
        class CachedTokenData
        {
            public OAuthTokenResponseData token;
            public AuthType authType;
        }

        class TokenCache
        {
            public List<CachedTokenData> tokens = new List<CachedTokenData>();
        }

        public static async Task<OAuthTokenResponseData> OAuthTryLocateCachedToken(ISimplexClient client, AuthServiceParams authParams)
        {
            var cache = await client.ClientCache.GetCache<TokenCache>();

            foreach (var ctok in cache.Data.tokens)
                if (ctok.authType == authParams.Type)
                    return ctok.token;

            return null;
        }

        public static async Task OAuthCacheToken(ISimplexClient client, OAuthTokenResponseData token, AuthServiceParams authParams)
        {
            CachedTokenData cachedToken = new CachedTokenData()
            {
                token = token,
                authType = authParams.Type
            };

            var cache = await client.ClientCache.GetCache<TokenCache>();
            await cache.Modify
                ((data) =>
                {
                    int ind = -1;
                    foreach (var tok in data.tokens)
                        if (tok.authType == cachedToken.authType)
                            ind = data.tokens.IndexOf(tok);
                    if (ind == -1)
                        data.tokens.Add(cachedToken);
                    else
                        data.tokens[ind] = cachedToken;
                });
        }

        public static async Task<OAuthAuthenticationData> OAuthAuthenticate(ISimplexClient client, AuthServiceParams authParams)
        {
            var logger = client.Config.Logger;
            logger.Info("OAuth - authenticating user...");

            var (redirect_uri, httpListener) = LocalHttpListenerGenerator.GenerateListener(65530, 65530);
            string authenticationUrl = authParams.CreateAuthenticationRequestURL(redirect_uri);

            logger.Debug($"redirect_uri: {redirect_uri}");
            logger.Info($"Sending user to {authenticationUrl}");

            var psi = new ProcessStartInfo
            {
                FileName = authenticationUrl,
                UseShellExecute = true
            };
            Process.Start(psi);

            logger.Info($"Listening for http response on {redirect_uri}");
            httpListener.Start();
            var ct = await httpListener.GetContextAsync();
            ct.Response.OutputStream.Write(Encoding.UTF8.GetBytes($"<head><title>{client.Config.GameName}</title></head><h1>please close this window</h1>"));
            ct.Response.OutputStream.Flush();

            OAuthAuthenticationData data = new OAuthAuthenticationData()
            {
                AuthCode = ct.Request.QueryString["code"],
                Scope = ct.Request.QueryString["scope"],
                RedirectURI = redirect_uri
            };

            logger.Info("Received authentication response!");
            logger.Debug($"auth code: {data.AuthCode}");
            logger.Debug($"scope: {data.Scope}");

            ct.Response.Close();
            httpListener.Stop();

            return data;
        }

        public static async Task<SimplexResponse<OAuthRequestTokenResponse>> OAuthRequestToken(ISimplexClient client, OAuthAuthenticationData authData, AuthServiceParams authParams)
        {
            var authRq = new OAuthRequestTokenRequest()
            {
                AuthenticationData = authData,
                AuthType = authParams.Type,
            };

            SimplexRequest rq = new SimplexRequest(SimplexRequestType.OAuth, authRq);

            var rsp = await client.SendRequest<OAuthRequestTokenResponse>(rq);

            if (!rsp.Error)
            {
                client.Config.Logger.Error(rsp.Error);
            }

            return rsp;
        }

        public static async Task<SimplexResponse<OAuthRequestTokenResponse>> OAuthRefreshToken(ISimplexClient client, OAuthTokenResponseData token, AuthServiceParams authParams)
        {
            var oauthRq = new OAuthRefreshTokenRequest()
            {
                AuthType = authParams.Type,
                Token = token
            };

            SimplexRequest rq = new SimplexRequest(SimplexRequestType.OAuth, oauthRq);

            var rsp = await client.SendRequest<OAuthRequestTokenResponse>(rq);

            if (!rsp.Error)
            {
                client.Config.Logger.Error(rsp.Error);
            }

            return rsp;
        }

        public static async Task<SimplexResponse<AuthResponse>> OAuthAuthAccount(ISimplexClient client, OAuthTokenResponseData tokenData, AuthServiceParams authParams)
        {
            OAuthRequestAuthAccount oauthRq = new OAuthRequestAuthAccount()
            {
                AuthType = authParams.Type,
                TokenData = tokenData,
            };

            SimplexRequest rq = new SimplexRequest(SimplexRequestType.Auth, oauthRq);

            var rsp = await client.SendRequest<AuthResponse>(rq);

            if (!rsp.Error)
            {
                client.Config.Logger.Error(rsp.Error);
            }

            return rsp;
        }
    }
}
