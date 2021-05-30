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
    public class AuthServiceParams
    {
        public AuthType Type { get; set; }
        public string AuthName { get; set; }
        public bool Enabled { get; set; }

        public string OAuthClientID { get; set; }
        public string OAuthAuthenticationURL { get; set; }
        public string OAuthScopeString { get; set; }

        protected AuthServiceParams() { }

        public string CreateAuthenticationRequestURL(string redirect_uri)
        {
            return OAuthAuthenticationURL
                .SetQueryParam("client_id", OAuthClientID)
                .SetQueryParam("redirect_uri", redirect_uri)
                .SetQueryParam("response_type", "code")
                .SetQueryParam("scope", OAuthScopeString);
        }
    }

    public class AuthServiceParamsLambda : AuthServiceParams
    {
        public string OAuthClientSecret { get; set; }
        public string OAuthTokenURL { get; set; }
        public int OAuthTokenResponseDataID { get; set; }
        public string OAuthAccountDataRequestURL { get; set; }
        public Dictionary<string, string> OAuthAdditionalQueryParameters { get; set; } = new Dictionary<string, string>();
        public string OAuthAccountDataAccountIDLocator { get; set; }
        public string OAuthAccountDataEmailAddressLocator { get; set; }

        public string CreateTokenRequestURL(string authCode, string redirect_uri)
        {
            return OAuthTokenURL
                .SetQueryParam("client_id", OAuthClientID)
                .SetQueryParam("client_secret", OAuthClientSecret)
                .SetQueryParam("code", authCode)
                .SetQueryParam("grant_type", "authorization_code")
                .SetQueryParam("redirect_uri", redirect_uri);
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

        public AuthServiceParams GetAuthParams(AuthType type)
        {
            foreach (var a in AuthParams)
                if (a.Type == type)
                    return a;

            return null;
        }
    }
}
