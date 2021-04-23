using System;
using System.Collections.Generic;
using System.Text;
using System.Security.Cryptography;
using System.Text.Json.Serialization;
using System.Text.Json;
using Simplex.Protocol;

namespace Simplex
{
    public class SimplexServiceConfig
    {
        public class OAuthURLParams
        {
            public AuthType Type { get; set; }
            public string AuthURL { get; set; }
            public string TokenURL { get; set; }
            public string ScopeString { get; set; }

            OAuthURLParams() { }
            public OAuthURLParams(AuthType type, string authURL, string tokenURL, string scopeString)
            {
                Type = type;
                AuthURL = authURL;
                TokenURL = tokenURL;
                ScopeString = scopeString;
            }
        }

        [JsonIgnore]
        public ValidatedConfigValue<string> PublicKeyXML 
        {
            get;
            set;
        } = new ValidatedConfigValue<string>(string.IsNullOrEmpty, (str) => str, "");
        [JsonIgnore]
        public ValidatedConfigValueComplex<List<OAuthURLParams>> OAuthURLs 
        {
            get;
            set;
        } = new ValidatedConfigValueComplex<List<OAuthURLParams>>((obj) => true, LoadOauthParams, new List<OAuthURLParams>());

        public string _publicKeyXML
        {
            get { return PublicKeyXML.ToValue(); }
            set { PublicKeyXML.SetValue(value); }
        }

        public List<OAuthURLParams> _oAuthURLs
        {
            get { return OAuthURLs.ToValue(); }
            set { OAuthURLs.SetValue(value); }
        }

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

        public static SimplexServiceConfig Load(string origName, Func<string, string> loadFunc)
        {
            SimplexServiceConfig cfg = new SimplexServiceConfig();
            cfg.LoadConfig(loadFunc);
            return cfg;
        }

        public static List<OAuthURLParams> LoadOauthParams(string origName, Func<string, string> loadFunc)
        {
            string str = loadFunc(origName);
            if (string.IsNullOrEmpty(str))
                return new List<OAuthURLParams>();

            return JsonSerializer.Deserialize<List<OAuthURLParams>>(str);
        }
    }
}
