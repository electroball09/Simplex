using System;
using System.Collections.Generic;
using System.Text;
using SimplexLambda.User;
using Simplex;
using System.Security.Cryptography;
using Simplex.Protocol;

namespace SimplexLambda
{
    public static class LambdaUtil
    {
        static HMACMD5 md5 = new HMACMD5();
        static SHA256 sha = SHA256.Create();

        public static SimplexError ValidateAccessToken(Guid guid, string tokenStr, SimplexRequestContext context)
        {
            const string diagName = "VALIDATE_ACCESS_TOKEN";
            context.DiagInfo.BeginDiag(diagName);

            UserAccessToken token = new UserAccessToken()
            {
                GUID = guid,
            };

            var err = context.DB.LoadItem(token, out token, context);

            if (err)
            {
                context.DiagInfo.EndDiag(diagName);

                if (err.Code == SimplexErrorCode.DBItemNonexistent)
                    return SimplexError.GetError(SimplexErrorCode.AccessTokenInvalid);

                return err;
            }

            if (tokenStr != token.Token)
            {
                context.DiagInfo.EndDiag(diagName);
                return SimplexError.GetError(SimplexErrorCode.AccessTokenInvalid);
            }

            if (DateTime.Now - token.LastAccessed > token.Timeout
                || DateTime.Now - token.Created > token.Duration)
            {
                context.DiagInfo.EndDiag(diagName);
                return SimplexError.GetError(SimplexErrorCode.AccessTokenInvalid);
            }

            token.LastAccessed = DateTime.Now;
            err = context.DB.SaveItem(token);

            context.DiagInfo.EndDiag(diagName);

            if (err)
            {
                return SimplexError.GetError(SimplexErrorCode.AccessTokenInvalid);
            }

            return SimplexError.OK;
        }

        public static (SimplexError err, UserAccessToken token) GenerateAccessToken(Guid guid, SimplexRequestContext context)
        {
            const string diagName = "GENERATE_ACCESS_TOKEN";
            context.DiagInfo.BeginDiag(diagName);

            Guid tokenGuid = Guid.NewGuid();
            byte[] hash = md5.ComputeHash(tokenGuid.ToByteArray());
            string token = BitConverter.ToString(hash).Replace("-", "");

            var newToken = new UserAccessToken()
            {
                GUID = guid,
                Token = token,
                Created = DateTime.Now,
                LastAccessed = DateTime.Now,
                Duration = TimeSpan.FromHours(context.LambdaConfig.CredentialDurationHours),
                Timeout = TimeSpan.FromMinutes(context.LambdaConfig.CredentialTimeoutMinutes)
            };

            context.DB.SaveItem(newToken);

            context.DiagInfo.EndDiag(diagName);

            return (SimplexError.OK, newToken);
        }

        public static string HashInput(string input, string salt)
        {
            string tmp = $"{input}{salt}";
            byte[] inData = Encoding.UTF8.GetBytes(tmp);
            byte[] data = sha.ComputeHash(inData);
            return Convert.ToBase64String(data);
        }
    }
}
