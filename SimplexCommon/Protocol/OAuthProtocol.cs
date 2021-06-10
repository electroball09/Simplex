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

    public class OAuthIDToken
    {
        public class Metadata
        {
            public string alg { get; set; }
            public string kid { get; set; }
            public string typ { get; set; }
        }

        public class IDFields
        {
            public string iss { get; set; }
            public string azp { get; set; }
            public string aud { get; set; }
            public string sub { get; set; }
            public int iat { get; set; }
            public int exp { get; set; }

            public string email { get; set; }
            public bool email_verified { get; set; }
        }

        public Metadata metadata { get; set; }
        public IDFields idFields { get; set; }
        public string signature { get; set; }

        public static implicit operator OAuthIDToken(string token)
        {
            token = token.Replace("_", "/").Replace("-", "+");
            var split = token.Split('.');
            for (int i = 0; i < split.Length; i++)
            {
                switch (split[i].Length % 4)
                {
                    case 2: split[i] += "=="; break;
                    case 3: split[i] += "="; break;
                }

                split[i] = Encoding.UTF8.GetString(Convert.FromBase64String(split[i]));
            }
            OAuthIDToken jwt = new OAuthIDToken()
            {
                metadata = JsonSerializer.Deserialize<Metadata>(split[0]),
                idFields = JsonSerializer.Deserialize<IDFields>(split[1]),
                signature = split[2]
            };
            return jwt;
        }

        public static implicit operator string(OAuthIDToken token)
        {
            return token.ToString();
        }

        public override string ToString()
        {
            string[] split = new string[3];
            split[0] = JsonSerializer.Serialize(metadata);
            split[1] = JsonSerializer.Serialize(idFields);
            split[2] = signature;

            for (int i = 0; i < split.Length; i++)
            {
                split[i] = Convert.ToBase64String(Encoding.UTF8.GetBytes(split[i]));
            }

            var str = string.Join('.', split);
            str = str.Replace("/", "_").Replace("+", "-");

            return str;
        }
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
        public string id_token { get => IDToken; set => IDToken = value; }
        public int expires_in { get; set; }
        public string Scopes { get; set; }

        [JsonIgnore]
        public OAuthIDToken IDToken { get; private set; }

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
                    Scopes = string.Join(' ', value);
            }
        }
    }

    public enum OAuthRequestType
    {
        None,
        RequestToken,
        RefreshToken
    }

    public class OAuthRequest : AuthRequest
    {
        public override AuthServiceIdentifier ServiceIdentifier 
        { 
            get => base.ServiceIdentifier; 
            set
            {
                if (!value.Type.HasFlag(AuthServiceIdentifier.AuthType.IsOAuth))
                    throw new InvalidOperationException("Cannot assign an auth type that does not have flag AuthType.IsOAuth");
                base.ServiceIdentifier = value;
            }
        }

        private OAuthRequestType _requestType;
        public virtual OAuthRequestType RequestType { get => _requestType; set => _requestType = value; }
    }

    public class OAuthRequestTokenRequest : OAuthRequest
    {
        public override OAuthRequestType RequestType { get => OAuthRequestType.RequestToken; set => base.RequestType = value; }

        public OAuthAuthenticationData AuthenticationData { get; set; }
    }

    public class OAuthTokenResponse
    {
        public OAuthTokenResponseData TokenData { get; set; }
    }

    public class OAuthRefreshTokenRequest : OAuthRequest
    {
        public override OAuthRequestType RequestType { get => OAuthRequestType.RefreshToken; set => base.RequestType = value; }

        public OAuthTokenResponseData Token { get; set; }
    }

    public class OAuthRequestAuthAccount : OAuthRequest
    {
        public OAuthTokenResponseData TokenData { get; set; }

        public override DateTime OverrideAuthExpiryUTC => TokenData.UTCTimeRequested + TimeSpan.FromSeconds(TokenData.expires_in);
    }
}
