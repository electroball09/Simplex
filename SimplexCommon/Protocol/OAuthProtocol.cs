using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.Json;
using Simplex.Util;

namespace Simplex.Protocol
{
    public class OAuthAuthenticationData
    {
        public string AuthCode { get; set; }
        public string Scope { get; set; }
        public string RedirectURI { get; set; }
    }

    public class OAuthTokenResponseData
    {
        [JsonIgnore]
        public static TypeMap<int> ResponseDataMap { get; }
            = new TypeMap<int>()
            {
                { 0, typeof(OAuthTokenResponseDataScopeString) },
                { 1, typeof(OAuthTokenResponseDataScopeArray) }
            };

        public string access_token { get; set; }
        public string refresh_token { get; set; }
        public string id_token { get; set; }
        public int expires_in { get; set; }
        public string Scopes { get; set; }

        public DateTime UTCTimeRequested { get; set; }

        public string error { get; set; }

        public override string ToString()
        {
            return $"{access_token}" +
                $"{refresh_token}" +
                $"{id_token}" +
                $"{expires_in}" +
                $"{Scopes}";
        }
    }

    public class OAuthTokenResponseDataScopeString : OAuthTokenResponseData
    {
        public string scope { get => Scopes; set => Scopes = value; }
    }

    public class OAuthTokenResponseDataScopeArray : OAuthTokenResponseData
    {
        public string[] scope { get => Scopes?.Split(' ');
            set
            {
                if (value != null)
                    Scopes = string.Join(' ', Scopes);
            }
        }
    }

    public class OAuthRequest : AuthRequest
    {
        private AuthType _authType;
        public override AuthType AuthType
        {
            get
            {
                return _authType;
            }
            set
            {
                if (!value.HasFlag(AuthType.IsOAuth))
                    throw new InvalidOperationException("Cannot assign an auth type that does not have flag AuthType.IsOAuth");
                _authType = value;
            }
        }
    }

    public class OAuthRequestTokenRequest : OAuthRequest
    {
        public OAuthAuthenticationData AuthenticationData { get; set; }
    }

    public class OAuthRequestTokenResponse
    {
        public OAuthTokenResponseData TokenData { get; set; }
    }

    public class OAuthRefreshTokenRequest : OAuthRequest
    {
        public OAuthTokenResponseData Token { get; set; }
    }

    public class OAuthRequestAuthAccount : OAuthRequest
    {
        public OAuthTokenResponseData TokenData { get; set; }
    }
}
