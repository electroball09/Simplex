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
    public partial class Routines
    {
        class CachedTokenData
        {
            public OAuthTokenResponseData token { get; set; }
            public AuthServiceIdentifier Identifier { get; set; }
        }

        class TokenCache
        {
            public List<CachedTokenData> tokens { get; set; } = new List<CachedTokenData>();
        }

        class LoggedInUser
        {
            public AuthResponse Credentials { get; set; }
        }

        public static async Task<OAuthTokenResponseData> OAuthTryLocateCachedToken(ISimplexClient client, AuthServiceIdentifier identifier)
        {
            var cache = await client.ClientCache.GetCache<TokenCache>();

            foreach (var ctok in cache.Data.tokens)
                if (ctok.Identifier == identifier)
                    return ctok.token;

            return null;
        }

        public static async Task OAuthCacheToken(ISimplexClient client, OAuthTokenResponseData token, AuthServiceIdentifier identifier)
        {
            CachedTokenData cachedToken = new CachedTokenData()
            {
                token = token,
                Identifier = identifier
            };

            var cache = await client.ClientCache.GetCache<TokenCache>();
            await cache.Modify
                ((data) =>
                {
                    int ind = -1;
                    foreach (var tok in data.tokens)
                        if (tok.Identifier == cachedToken.Identifier)
                            ind = data.tokens.IndexOf(tok);

                    if (ind == -1)
                        data.tokens.Add(cachedToken);
                    else
                        data.tokens[ind] = cachedToken;
                });
        }

        public static async Task CacheLoggedInUser(ISimplexClient client, AuthResponse loggedInUser)
        {
            var cache = await client.ClientCache.GetCache<LoggedInUser>();

            await cache.Modify((obj) => obj.Credentials = loggedInUser);
        }
    }
}
