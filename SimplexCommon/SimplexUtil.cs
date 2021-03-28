using System;
using System.Collections.Generic;
using System.Text;
using System.Security.Cryptography;

namespace Simplex
{
    public static class SimplexUtil
    {
        public static string EncryptData(RSACryptoServiceProvider rsa, string str)
        {
            byte[] strBytes = Encoding.UTF8.GetBytes(str);
            byte[] encBytes = rsa.Encrypt(strBytes, false);
            return Convert.ToBase64String(encBytes);
        }

        public static (SimplexError err, string decryptedStr) DecryptString(RSACryptoServiceProvider rsa, string str)
        {
            try
            {
                byte[] encBytes = Convert.FromBase64String(str);
                byte[] decBytes = rsa.Decrypt(encBytes, false);
                return (SimplexError.OK, Encoding.UTF8.GetString(decBytes));
            }
            catch (CryptographicException ex)
            {
                return (SimplexError.GetError(SimplexErrorCode.InvalidCryptographyConfiguration, ex.Message), null);
            }
        }
    }
}
