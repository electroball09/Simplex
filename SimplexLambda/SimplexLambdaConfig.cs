using Simplex;
using System;
using System.Collections.Generic;
using System.Text;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using Simplex.Util;
using System.IO;
using Simplex.Serialization;
using Simplex.Protocol;

namespace SimplexLambda
{
    public class SimplexLambdaConfig
    {
        public struct RollingConfig
        {
            public string RSAKeyHex { get; set; }
            public string AESKeyHex { get; set; }
        }

        [MinLength(1)]
        public string SimplexTable { get; set; } = "";
        public bool DetailedErrors { get; set; } = false;
        public bool IncludeDiagnosticInfo { get; set; } = false;
        [Range(1, 72)]
        public int TokenExpirationHours { get; set; } = 6;
        [MinLength(0)]
        public AuthServiceParamsLambda[] AuthParams { get; set; } = new AuthServiceParamsLambda[0];
        [Range(1, 72)]
        public int RollingConfigExpirationHours { get; set; } = 24;

        [ConfigClassValidator, JsonIgnore]
        public SimplexServiceConfig ServiceConfig { get; set; }

        [JsonIgnore]
        public RSACryptoServiceProvider PrivateRSA { get; private set; }
        [JsonIgnore]
        public Aes AES { get; private set; }

        private static DateTime lastRollingConfigUpdate;
        private static RollingConfig rollingConfig;

        public static SimplexLambdaConfig Load(Func<string, string> varFunc, SimplexDiagnostics diag)
        {
            var handle = diag.BeginDiag("LOAD_CONFIG");

            string json = varFunc("config");

            SimplexLambdaConfig cfg = JsonSerializer.Deserialize<SimplexLambdaConfig>(json);

            cfg.AuthParams = new AuthServiceParamsLambda[]
            {
            };

            cfg.ServiceConfig = new SimplexServiceConfig()
            {
                AuthParams = cfg.AuthParams
            };

            cfg.UpdateRollingConfig(varFunc, diag);

            diag.EndDiag(handle);

            return cfg;
        }

        public void UpdateRollingConfig(Func<string, string> envVarFunc, SimplexDiagnostics diag)
        {
            var handle = diag.BeginDiag("UPDATE_ROLLING_CONFIG");

            string date = Environment.GetEnvironmentVariable("configUpdated");
            DateTime lastUpdateDate = new DateTime();
            if (!string.IsNullOrEmpty(date))
                lastUpdateDate = DateTime.Parse(date);

            if (lastUpdateDate >= lastRollingConfigUpdate)
            {
                rollingConfig = JsonSerializer.Deserialize<RollingConfig>(envVarFunc("rollingConfig"));
                UpdateEncryptionFromRollingConfig(diag);
            }

            diag.EndDiag(handle);
        }

        private void UpdateEncryptionFromRollingConfig(SimplexDiagnostics diag)
        {
            var handle = diag.BeginDiag("UPDATE_ENCRYPTION");

            RSACryptoServiceProvider rsa = new RSACryptoServiceProvider();
            Aes aes = new AesManaged();

            RSASerializer rsaSer = new RSASerializer(rsa, true);
            AESSerializer aesSer = new AESSerializer(aes);

            Span<byte> rsaBytes = rollingConfig.RSAKeyHex.ToHexBytes();
            Span<byte> aesBytes = rollingConfig.AESKeyHex.ToHexBytes();
            rsaBytes.CopyTo(SimplexUtil.buffer.AsSpan(0, rsaBytes.Length));
            aesBytes.CopyTo(SimplexUtil.buffer.AsSpan(rsaBytes.Length, aesBytes.Length));

            using (MemoryStream ms = new MemoryStream(SimplexUtil.buffer))
            {
                rsaSer.SmpRead(ms);
                aesSer.SmpRead(ms);

                ms.Position = 0;
                rsaSer.SerializePrivateValues = false;
                rsaSer.SmpWrite(ms);
                ServiceConfig.PublicKeyHex = SimplexUtil.buffer.AsSpan(0, (int)rsaSer.SmpSize()).ToHexString();
            }

            PrivateRSA = rsa;
            AES = aes;

            lastRollingConfigUpdate = DateTime.UtcNow;

            diag.EndDiag(handle);
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

        public AuthServiceParamsLambda GetAuthParams(AuthType authType)
        {
            foreach (var p in AuthParams)
                if (p.Type == authType)
                    return p;

            return null;
        }
    }
}
