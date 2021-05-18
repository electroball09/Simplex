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
        public static SimplexError ValidateAccessToken(SimplexAccessToken token, SimplexAccessFlags requiredFlags, SimplexRequestContext context, out SimplexError err)
        {
            var handle = context.DiagInfo.BeginDiag("VALIDATE_ACCESS_TOKEN");

            if (DateTime.UtcNow - token.Created > TimeSpan.FromHours(context.LambdaConfig.TokenExpirationHours))
            {
                err = SimplexError.GetError(SimplexErrorCode.AccessTokenExpired);
            }
            else
            {
                if ((token.AccessFlags & requiredFlags) != requiredFlags)
                {
                    err = SimplexError.GetError(SimplexErrorCode.PermissionDenied);
                }
                else
                {
                    err = SimplexError.OK;
                }
            }

            context.DiagInfo.EndDiag(handle);
            return err;
        }

        public static SimplexError GenerateAccessToken(Guid guid, SimplexRequestContext context, out SimplexAccessToken accessToken, out SimplexError err)
        {
            var diagHandle = context.DiagInfo.BeginDiag("GENERATE_ACCESS_TOKEN");

            accessToken = new SimplexAccessToken()
            {
                Created = DateTime.UtcNow,
                UserGUID = guid
            };

            context.DiagInfo.EndDiag(diagHandle);

            err = SimplexError.OK;
            return err;
        }

        public static string HashInput(HashAlgorithm hash, string input, string salt)
        {
            string tmp = $"{input}{salt}";
            byte[] inData = Encoding.UTF8.GetBytes(tmp);
            byte[] data = hash.ComputeHash(inData);
            return Convert.ToBase64String(data);
        }
    }
}
