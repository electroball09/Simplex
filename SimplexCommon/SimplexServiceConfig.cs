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

                    var b = PublicKeyHex.ToHexBytes().ToArray();
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
