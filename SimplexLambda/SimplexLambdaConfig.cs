using Simplex;
using System;
using System.Collections.Generic;
using System.Text;
using System.Security.Cryptography;
using System.Text.Json;
using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace SimplexLambda
{

    public class SimplexLambdaConfig
    {
        public ValidatedConfigValue<string> SimplexTable { get; } = new ValidatedConfigValue<string>(string.IsNullOrEmpty, (str) => str);
        public ValidatedConfigValue<string> PrivateKeyXML { get; } = new ValidatedConfigValue<string>(string.IsNullOrEmpty, (str) => str);
        public ValidatedConfigValue<bool> DetailedErrors { get; } = new ValidatedConfigValue<bool>((obj) => true, (str) => str == "true");
        public ValidatedConfigValue<bool> IncludeDiagnosticInfo { get; } = new ValidatedConfigValue<bool>((obj) => true, (str) => str == "true");
        public ValidatedConfigValue<int> CredentialTimeoutMinutes { get; } = new ValidatedConfigValue<int>((num) => num > 0, (str) => { int.TryParse(str, out int value); return value; }, 0);
        public ValidatedConfigValue<int> CredentialDurationHours { get; } = new ValidatedConfigValue<int>((num) => num > 0, (str) => { int.TryParse(str, out int value); return value; }, 0);
        //public string SimplexTable { get; private set; }
        //public string PrivateKeyXML { get; private set; }
        //public bool DetailedErrors { get; private set; }
        //public bool IncludeDiagnosticInfo { get; private set; }
        //public int CredentialTimeoutMinutes { get; private set; }
        //public int CredentialDurationHours { get; private set; }

        public ValidatedConfigValueComplex<SimplexServiceConfig> ServiceConfig { get; } = new ValidatedConfigValueComplex<SimplexServiceConfig>
            (SimplexValidator.ValidateObject,
            SimplexServiceConfig.Load);
        //public SimplexServiceConfig ServiceConfig { get; private set; }

        public RSACryptoServiceProvider RSA { get; private set; }

        public void Load(Func<string, string> varFunc = null)
        {
            if (varFunc == null)
                varFunc = Environment.GetEnvironmentVariable;

            this.LoadConfig(varFunc);

            //PrivateKeyXML = varFunc("PrivateKeyXML");
            //RSA = new RSACryptoServiceProvider();
            //RSA.FromXmlString(PrivateKeyXML);

            //SimplexTable = varFunc("SimplexTable");

            //DetailedErrors = varFunc("DetailedErrors") == "true";
            //IncludeDiagnosticInfo = varFunc("IncludeDiagnosticInfo") == "true";

            //int.TryParse(varFunc("CredentialTimeoutMinutes"), out int timeout);
            //CredentialTimeoutMinutes = timeout;
            //int.TryParse(varFunc("CredentialDurationHours"), out int duration);
            //CredentialDurationHours = duration;

            //ServiceConfig = new SimplexServiceConfig()
            //{
            //    CryptKeyXML = varFunc("PublicKeyXML"),
            //    OAuthURLs = JsonSerializer.Deserialize<Dictionary<Simplex.Protocol.AuthType, SimplexServiceConfig.OAuthURLParams>>(varFunc("OAuthURLs"))
            //};
        }

        public bool ValidateConfig()
        {
            return this.ValidateObject();

            //if (string.IsNullOrEmpty(PrivateKeyXML)) return false;
            //if (string.IsNullOrEmpty(ServiceConfig.CryptKeyXML)) return false;
            //if (string.IsNullOrEmpty(SimplexTable)) return false;
            //if (CredentialTimeoutMinutes <= 0
            //    || CredentialDurationHours <= 0) return false;

            //return true;
        }
    }
}
