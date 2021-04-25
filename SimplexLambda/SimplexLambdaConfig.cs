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
        [MinLength(1), ConfigValueString]
        public string SimplexTable { get; private set; } = "";
        [MinLength(1), ConfigValueString]
        public string PrivateKeyXML { get; private set; } = "";
        [ConfigValueBool]
        public bool DetailedErrors { get; private set; } = false;
        [ConfigValueBool]
        public bool IncludeDiagnosticInfo { get; private set; } = false;
        [Range(1, int.MaxValue), ConfigValueInt]
        public int CredentialTimeoutMinutes { get; private set; } = 10;
        [Range(1, int.MaxValue), ConfigValueInt]
        public int CredentialDurationHours { get; private set; } = 8;

        [ConfigClass]
        public SimplexServiceConfig ServiceConfig { get; private set; }

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

        List<ValidationResult> results;

        private SimplexError ProcessResults(List<ValidationResult> list)
        {
            if (list == null || list.Count == 0)
                return SimplexError.OK;

            StringBuilder b = new StringBuilder();
            b.AppendLine("Lambda config validation failed!");
            foreach (var r in list)
                b.AppendLine(r.ToString());

            return SimplexError.GetError(SimplexErrorCode.LambdaMisconfiguration, b.ToString());
        }

        public SimplexError ValidateConfig()
        {
            if (results == null)
            {
                ValidationContext ct = new ValidationContext(this);
                var res = new List<ValidationResult>();

                bool success = Validator.TryValidateObject(this, ct, res);

                if (success)
                    results = new List<ValidationResult>();
                else
                    results = res;
            }

            return ProcessResults(results);

            //if (string.IsNullOrEmpty(PrivateKeyXML)) return false;
            //if (string.IsNullOrEmpty(ServiceConfig.CryptKeyXML)) return false;
            //if (string.IsNullOrEmpty(SimplexTable)) return false;
            //if (CredentialTimeoutMinutes <= 0
            //    || CredentialDurationHours <= 0) return false;

            //return true;
        }
    }
}
