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

        [ConfigClass, ConfigClassValidator]
        public SimplexServiceConfig ServiceConfig { get; private set; } = new SimplexServiceConfig();

        public RSACryptoServiceProvider RSA { get; private set; }

        public void Load(Func<string, string> varFunc)
        {
            this.LoadConfig(varFunc);
        }

        List<ValidationResult> results;
        string errorStr = null;

        private SimplexError ProcessResults(List<ValidationResult> list)
        {
            if (list.Count == 0)
            {
                if (RSA == null)
                {
                    RSA = new RSACryptoServiceProvider();
                    RSA.FromXmlString(PrivateKeyXML);
                }

                return SimplexError.OK;
            }
            if (errorStr == null)
            {
                StringBuilder b = new StringBuilder();
                b.AppendLine("Lambda config validation failed!");
                foreach (var r in list)
                    b.AppendLine(r.ToString());
                errorStr = b.ToString();
            }

            return SimplexError.GetError(SimplexErrorCode.LambdaMisconfiguration, errorStr);
        }

        public SimplexError ValidateConfig()
        {
            if (results == null)
            {
                ValidationContext ct = new ValidationContext(this);
                var res = new List<ValidationResult>();

                bool success = Validator.TryValidateObject(this, ct, res, true);

                if (success)
                    results = new List<ValidationResult>();
                else
                    results = res;
            }

            return ProcessResults(results);
        }
    }
}
