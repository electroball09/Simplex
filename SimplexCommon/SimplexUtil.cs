using System;
using System.Collections.Generic;
using System.Text;
using System.Security.Cryptography;

namespace Simplex
{
    public static class SimplexUtil
    {
        public static SimplexError EncryptData(RSACryptoServiceProvider rsa, string stringToEncrypt, out string encryptedString, out SimplexError err)
        {
            try
            {
                byte[] strBytes = Encoding.UTF8.GetBytes(stringToEncrypt);
                byte[] encBytes = rsa.Encrypt(strBytes, false);
                encryptedString = Convert.ToBase64String(encBytes);
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
                byte[] encBytes = Convert.FromBase64String(stringToDecrypt);
                byte[] decBytes = rsa.Decrypt(encBytes, false);
                err = SimplexError.OK;
                decryptedString = Encoding.UTF8.GetString(decBytes);
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
