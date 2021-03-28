using Simplex;
using System;
using System.Collections.Generic;
using System.Text;
using System.Security.Cryptography;

namespace SimplexLambda
{
    public class SimplexLambdaConfig
    {
        public string SimplexTable { get; private set; }
        public string PrivateKeyXML { get; private set; }
        public bool DetailedErrors { get; private set; }
        public bool IncludeDiagnosticInfo { get; private set; }
        public int CredentialTimeoutMinutes { get; private set; }
        public int CredentialDurationHours { get; private set; }

        public SimplexServiceConfig ServiceConfig { get; private set; }

        public RSACryptoServiceProvider RSA { get; private set; }

        public void LoadConfig()
        {
            PrivateKeyXML = Environment.GetEnvironmentVariable("PrivateKeyXML");
            RSA = new RSACryptoServiceProvider();
            RSA.FromXmlString(PrivateKeyXML);

            SimplexTable = Environment.GetEnvironmentVariable("SimplexTable");
            DetailedErrors = Environment.GetEnvironmentVariable("DetailedErrors") == "true";
            IncludeDiagnosticInfo = Environment.GetEnvironmentVariable("IncludeDiagnosticInfo") == "true";

            int.TryParse(Environment.GetEnvironmentVariable("CredentialTimeoutMinutes"), out int timeout);
            CredentialTimeoutMinutes = timeout;
            int.TryParse(Environment.GetEnvironmentVariable("CredentialDurationHours"), out int duration);
            CredentialDurationHours = duration;

            ServiceConfig = new SimplexServiceConfig()
            {
                CryptKeyXML = Environment.GetEnvironmentVariable("PublicKeyXML"),
            };
        }

        public bool VerifyConfig()
        {
            if (string.IsNullOrEmpty(PrivateKeyXML)) return false;
            if (string.IsNullOrEmpty(ServiceConfig.CryptKeyXML)) return false;
            if (string.IsNullOrEmpty(SimplexTable)) return false;
            if (CredentialTimeoutMinutes <= 0
                || CredentialDurationHours <= 0) return false;

            return true;
        }
    }
}
