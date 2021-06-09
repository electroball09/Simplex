using System;
using System.Collections.Generic;
using System.Text;
using System.Security.Cryptography;
using System.Text.Json.Serialization;
using System.Text.Json;
using Simplex.Protocol;
using System.ComponentModel.DataAnnotations;
using Simplex.Util;
using System.IO;
using Simplex.Serialization;
using Flurl;

namespace Simplex
{
    public class AuthServiceIdentifier : ISmpSerializer
    {
        public enum AuthType : byte
        {
            IsOAuth = 0b10000000,

            Basic = 0x01,
            Google = 0x02 | IsOAuth,
            Steam = 0x03,
            Twitch = 0x04 | IsOAuth,

            MAX = 0x80,
            INVALID = 0x00
        }

        private byte _type;
        public AuthType Type { get => (AuthType)_type; set => _type = (byte)value; }
        private StringWithLength _name = new StringWithLength("");
        public string Name { get => _name; set => _name.SetString(value); }

        [JsonIgnore]
        public bool IsOAuth { get => Type.HasFlag(AuthType.IsOAuth); }

        public void Serialize(SmpSerializationStructure repo)
        {
            repo.Byte(ref _type);
            repo.Serializer(ref _name);
        }

        public override string ToString()
        {
            return $"{Type} - {Name}";
        }

        public override int GetHashCode()
        {
            return _name.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (!(obj is AuthServiceIdentifier identifier))
                return false;

            return identifier.Name == Name
                && identifier.Type == Type;
        }
        
        public static bool operator ==(AuthServiceIdentifier a, AuthServiceIdentifier b)
        {
            return a.Equals(b);
        }

        public static bool operator !=(AuthServiceIdentifier a, AuthServiceIdentifier b)
        {
            return !(a == b);
        }
    }

    public class AuthServiceParams
    {
        public AuthServiceIdentifier Identifier { get; set; } = new AuthServiceIdentifier();
        public bool Enabled { get; set; }

        public string OAuthClientID { get; set; }
        public string OAuthAuthenticationURL { get; set; }
        public string OAuthClaimsString { get; set; }
        public string OAuthScopeString { get; set; }

        protected AuthServiceParams() { }

        public string CreateAuthenticationRequestURL(string redirect_uri)
        {
            return OAuthAuthenticationURL
                .SetQueryParam("client_id", OAuthClientID)
                .SetQueryParam("redirect_uri", redirect_uri)
                .SetQueryParam("response_type", "code")
                .SetQueryParam("scope", OAuthScopeString)
                .SetQueryParam("claims", OAuthClaimsString, isEncoded: true);
        }
    }

    public class AuthServiceParamsLambda : AuthServiceParams
    {
        public string OAuthClientSecret { get; set; }
        public string OAuthTokenURL { get; set; }
        public int OAuthTokenResponseDataTypeID { get; set; }

        public string CreateTokenRequestURL(string authCode, string redirect_uri)
        {
            return OAuthTokenURL
                .SetQueryParam("client_id", OAuthClientID)
                .SetQueryParam("client_secret", OAuthClientSecret)
                .SetQueryParam("code", authCode)
                .SetQueryParam("grant_type", "authorization_code")
                .SetQueryParam("redirect_uri", redirect_uri);
        }

        public string CreateTokenRefreshURL(string refresh_token)
        {
            return OAuthTokenURL
                .SetQueryParam("client_id", OAuthClientID)
                .SetQueryParam("client_secret", OAuthClientSecret)
                .SetQueryParam("grant_type", "refresh_token")
                .SetQueryParam("refresh_token", refresh_token);
        }
    }

    public class SimplexServiceConfig
    {
        [MinLength(1)]
        public string PublicKeyHex { get; set; } = "";
        [MinLength(0)]
        public AuthServiceParams[] AuthParams { get; set; } = new AuthServiceParams[0];

        private RSACryptoServiceProvider _rsa;
        [JsonIgnore]
        public RSACryptoServiceProvider RSA
        {
            get
            {
                if (string.IsNullOrEmpty(PublicKeyHex))
                    throw new InvalidOperationException("Trying to access RSA crypto before encryption key is loaded!");

                if (_rsa == null)
                {
                    _rsa = new RSACryptoServiceProvider();
                    RSASerializer rsaSer = new RSASerializer(_rsa, false);

                    var b = PublicKeyHex.ToHexBytes();
                    using (MemoryStream ms = new MemoryStream(b))
                    {
                        rsaSer.SmpRead(ms);
                    }
                }

                return _rsa;
            }
        }

        public AuthServiceParams GetParamsByType(AuthServiceIdentifier.AuthType type)
        {
            foreach (var a in AuthParams)
                if (a.Identifier.Type == type)
                    return a;

            return null;
        }

        public AuthServiceParams GetParamsByIdentifier(AuthServiceIdentifier identifier)
        {
            foreach (var a in AuthParams)
                if (a.Identifier == identifier)
                    return a;

            return null;
        }
    }
}
