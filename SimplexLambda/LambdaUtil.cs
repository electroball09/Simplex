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

        public static SimplexError ValidateAccessToken(Guid guid, string tokenStr, SimplexRequestContext context, out SimplexError err)
        {
            var diagHandle = context.DiagInfo.BeginDiag("VALIDATE_ACCESS_TOKEN");

            UserAccessToken token = new UserAccessToken()
            {
                GUID = guid,
            };

            if (!context.DB.LoadItem(token, out token, context, out err))
            {
                if (err.Code == SimplexErrorCode.DBItemNonexistent)
                    err = SimplexError.GetError(SimplexErrorCode.AccessTokenInvalid);
                goto end;
            }

            if (tokenStr != token.Token)
            {
                err = SimplexError.GetError(SimplexErrorCode.AccessTokenInvalid);
                goto end;
            }

            if (DateTime.Now - token.LastAccessed > token.Timeout
                || DateTime.Now - token.Created > token.Duration)
            {
                err = SimplexError.GetError(SimplexErrorCode.AccessTokenInvalid);
                goto end;
            }

            token.LastAccessed = DateTime.Now;
            err = context.DB.SaveItem(token);

            if (!err)
            {
                err = SimplexError.GetError(SimplexErrorCode.AccessTokenInvalid);
                goto end;
            }

            context.DiagInfo.EndDiag(diagHandle);

            return SimplexError.OK;

        end:
            context.DiagInfo.EndDiag(diagHandle);
            return err;
        }

        public static SimplexError GenerateAccessToken(Guid guid, SimplexRequestContext context, out UserAccessToken accessToken, out SimplexError err)
        {
            var diagHandle = context.DiagInfo.BeginDiag("GENERATE_ACCESS_TOKEN");

            Guid tokenGuid = Guid.NewGuid();
            byte[] hash = md5.ComputeHash(tokenGuid.ToByteArray());
            string token = BitConverter.ToString(hash).Replace("-", "");

            accessToken = new UserAccessToken()
            {
                GUID = guid,
                Token = token,
                Created = DateTime.Now,
                LastAccessed = DateTime.Now,
                Duration = TimeSpan.FromHours(context.LambdaConfig.CredentialDurationHours),
                Timeout = TimeSpan.FromMinutes(context.LambdaConfig.CredentialTimeoutMinutes)
            };

            context.DB.SaveItem(accessToken);

            context.DiagInfo.EndDiag(diagHandle);

            err = SimplexError.OK;
            return err;
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
