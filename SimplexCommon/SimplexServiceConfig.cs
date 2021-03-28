using System;
using System.Collections.Generic;
using System.Text;
using System.Security.Cryptography;
using System.Text.Json.Serialization;

namespace Simplex
{
    public class SimplexServiceConfig
    {
        public string CryptKeyXML { get; set; }

        private RSACryptoServiceProvider _rsa;
        [JsonIgnore]
        public RSACryptoServiceProvider RSA
        {
            get
            {
                if (string.IsNullOrEmpty(CryptKeyXML))
                    throw new InvalidOperationException("Trying to access RSA crypto before encryption key is loaded!");

                if (_rsa == null)
                {
                    _rsa = new RSACryptoServiceProvider();
                    _rsa.FromXmlString(CryptKeyXML);
                }

                return _rsa;
            }
        }
    }
}
