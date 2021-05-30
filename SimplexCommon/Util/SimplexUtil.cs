using System;
using System.Collections.Generic;
using System.Text;
using System.Security.Cryptography;

namespace Simplex.Util
{
    public static class SimplexUtil
    {
        const string hex_lookup = "0123456789ABCDEF";
        public static readonly byte[] buffer = new byte[10 * 1024]; //10kb is probably good

        public static string ToHexString(this Span<byte> bytes)
        {
            Span<char> chars = stackalloc char[bytes.Length * 2];

            for (int i = 0; i < bytes.Length; i++)
            {
                int ind = i * 2;
                byte val = bytes[i];
                chars[ind] = hex_lookup[(val & 0xF0) >> 4];
                chars[ind + 1] = hex_lookup[val & 0x0F];
            }

            return new string(chars);
        }

        public static byte[] ToHexBytes(this string str)
        {
            if (str.Length % 2 != 0)
                throw new FormatException($"Input string length must be divisible evenly by two ({str.Length})");

            ReadOnlySpan<char> chars = str.AsSpan();
            byte[] bytes = new byte[chars.Length / 2];

            for (int i = 0; i < bytes.Length; i++)
            {
                int ind = i * 2;
                char c1 = chars[ind + 1];
                char c2 = chars[ind];
                byte v1 = 0xFF;
                byte v2 = 0xFF;
                for (int j = 0; j < hex_lookup.Length; j++)
                {
                    char hex = hex_lookup[j];
                    if (hex == c1)
                        v1 = (byte)j;
                    if (hex == c2)
                        v2 = (byte)(j << 4);
                }
                if (v1 == 0xFF)
                    throw new FormatException($"Char '{c1}' is not a valid hexadecimal value");
                if (v2 == 0xFF)
                    throw new FormatException($"Char '{c2}' is not a valid hexadecimal value");
                bytes[i] = (byte)(v1 | v2);
            }

            return bytes;
        }

        public static SimplexError EncryptString(RSACryptoServiceProvider rsa, string stringToEncrypt, out string encryptedString, out SimplexError err)
        {
            try
            {
                Span<byte> sp = stackalloc byte[Encoding.UTF8.GetByteCount(stringToEncrypt)];
                Encoding.UTF8.GetBytes(stringToEncrypt, sp);
                rsa.TryEncrypt(sp, buffer.AsSpan(), RSAEncryptionPadding.Pkcs1, out int numBytes);
                encryptedString = buffer.AsSpan(0, numBytes).ToHexString();
                err = SimplexError.OK;
            }
            catch (CryptographicException ex)
            {
                err = SimplexError.GetError(SimplexErrorCode.InvalidCryptographyConfiguration, ex.Message);
                encryptedString = null;
            }
            return err;
        }

        public static SimplexError DecryptString(RSACryptoServiceProvider rsa, string stringToDecrypt, out string decryptedString, out SimplexError err)
        {
            try
            {
                Span<byte> decBytes = stringToDecrypt.ToHexBytes();
                rsa.TryDecrypt(decBytes, buffer.AsSpan(), RSAEncryptionPadding.Pkcs1, out int numBytes);
                err = SimplexError.OK;
                decryptedString = Encoding.UTF8.GetString(buffer.AsSpan(0, numBytes));
            }
            catch (CryptographicException ex)
            {
                err = SimplexError.GetError(SimplexErrorCode.InvalidCryptographyConfiguration, ex.Message);
                decryptedString = null;
            }
            return err;
        }
    }
}
