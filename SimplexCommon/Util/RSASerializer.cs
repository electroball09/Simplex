using System;
using System.Collections.Generic;
using System.Text;
using Simplex.Serialization;
using System.Security.Cryptography;

namespace Simplex.Util
{
    public class RSASerializer : ISmpSerializer
    {
        RSA _rsa;

        public bool SerializePrivateValues { get; set; }

        public RSASerializer(RSA rsa, bool includePrivate)
        {
            _rsa = rsa;
            SerializePrivateValues = includePrivate;
        }
        public RSASerializer(bool includePrivate) : this(new RSACryptoServiceProvider(), includePrivate) { }

        public void Serialize(SmpSerializationStructure repo)
        {
            var rsaParams = _rsa.ExportParameters(SerializePrivateValues);

            repo.Bytes(ref rsaParams.Exponent);
            repo.Bytes(ref rsaParams.Modulus);

            if (SerializePrivateValues)
            {
                repo.Bytes(ref rsaParams.D);
                repo.Bytes(ref rsaParams.DP);
                repo.Bytes(ref rsaParams.DQ);
                repo.Bytes(ref rsaParams.P);
                repo.Bytes(ref rsaParams.Q);
                repo.Bytes(ref rsaParams.InverseQ);
            }

            if (repo.IsRead)
                _rsa.ImportParameters(rsaParams);
        }
    }
}
