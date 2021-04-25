using System;
using System.Collections.Generic;
using System.Text;
using System.Security.Cryptography;
using System.Text.Json.Serialization;
using System.Text.Json;
using Simplex.Protocol;
using System.ComponentModel.DataAnnotations;

namespace Simplex
{
    public class AuthServiceParams
    {
        public AuthType Type { get; set; }
        public bool Enabled { get; set; }
        public string OAuthAuthenticationURL { get; set; }
        public string OAuthTokenURL { get; set; }
        public string OAuthScopeString { get; set; }

        AuthServiceParams() { }
    }

    public class SimplexServiceConfig
    {

        [MinLength(1), ConfigValueString]
        public string PublicKeyXML { get; set; } = "";
        [MinLength(0), ConfigValueJson(typeof(AuthServiceParams[]))]
        public AuthServiceParams[] AuthParams { get; set; } = new AuthServiceParams[0];

        private RSACryptoServiceProvider _rsa;
        [JsonIgnore]
        public RSACryptoServiceProvider RSA
        {
            get
            {
                if (string.IsNullOrEmpty(PublicKeyXML))
                    throw new InvalidOperationException("Trying to access RSA crypto before encryption key is loaded!");

                if (_rsa == null)
                {
                    _rsa = new RSACryptoServiceProvider();
                    _rsa.FromXmlString(PublicKeyXML);
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
