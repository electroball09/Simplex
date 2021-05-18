using System;
using System.Collections.Generic;
using System.Text;
using Simplex.Serialization;
using System.Security.Cryptography;

namespace Simplex.Util
{
    public class AESSerializer : ISmpSerializer
    {
        Aes _aes;

        public AESSerializer(Aes aes) => _aes = aes;

        public void Serialize(SmpSerializationStructure repo)
        {
            byte[] iv = new byte[16];
            _aes.IV.CopyTo(iv, 0);

            repo.Bytes(ref iv);

            int keySize = _aes.KeySize;
            repo.Int32(ref keySize);

            byte[] key = new byte[keySize / 8];
            if (_aes.KeySize == keySize)
                _aes.Key.CopyTo(key, 0);

            repo.Bytes(ref key);

            _aes.KeySize = keySize;
            _aes.Key = key;
            _aes.IV = iv;
        }
    }
}
