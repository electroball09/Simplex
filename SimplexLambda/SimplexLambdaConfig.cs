using Simplex;
using System;
using System.Collections.Generic;
using System.Text;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace SimplexLambda
{

    public class SimplexLambdaConfig
    {
        [MinLength(1)]
        public string SimplexTable { get; set; } = "";
        public bool DetailedErrors { get; set; } = false;
        public bool IncludeDiagnosticInfo { get; set; } = false;
        [Range(1, int.MaxValue)]
        public int TokenExpirationHours { get; set; } = 5;
        [MinLength(0)]
        public AuthServiceParams[] AuthParams { get; set; } = new AuthServiceParams[0];

        [ConfigClassValidator, JsonIgnore]
        public SimplexServiceConfig ServiceConfig { get; set; }

        [JsonIgnore]
        public RSACryptoServiceProvider RSA { get; private set; }
        [JsonIgnore]
        public Aes AES { get; private set; }

        public static SimplexLambdaConfig Load(Func<string, string> varFunc)
        {
            string json = varFunc("config");

            Console.WriteLine(json);

            SimplexLambdaConfig cfg = JsonSerializer.Deserialize<SimplexLambdaConfig>(json);

            Console.WriteLine($"table: {cfg.SimplexTable}");

            cfg.AES = new AesManaged();
            cfg.AES.KeySize = 128;
            cfg.AES.GenerateIV();
            cfg.AES.GenerateKey();

            cfg.RSA = new RSACryptoServiceProvider(1024);

            cfg.ServiceConfig = new SimplexServiceConfig()
            {
                AuthParams = cfg.AuthParams,
                PublicKeyXML = cfg.RSA.ToXmlString(false)
            };

            return cfg;
        }

        List<ValidationResult> results;
        string errorStr = null;

        private SimplexError ProcessResults(List<ValidationResult> list)
        {
            if (list.Count == 0)
            {
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
