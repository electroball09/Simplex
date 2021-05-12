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

            //if (tokenStr != token._token)
            //{
            //    err = SimplexError.GetError(SimplexErrorCode.AccessTokenInvalid);
            //    goto end;
            //}

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
            byte[] hash = context.SHA.ComputeHash(tokenGuid.ToByteArray());
            string token = BitConverter.ToString(hash).Replace("-", "");

            accessToken = new UserAccessToken()
            {
                GUID = guid,
                //_token = token,
                Created = DateTime.Now,
                LastAccessed = DateTime.Now,
                //Duration = TimeSpan.FromHours(context.LambdaConfig.CredentialDurationHours),
                Timeout = TimeSpan.FromMinutes(context.LambdaConfig.TokenExpirationHours)
            };

            //context.DB.SaveItem(accessToken);

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

        public static string ToHexString(this Span<byte> bytes)
        {
            const string hex_lookup = "0123456789ABCDEF";

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

        public static Span<byte> ToHexBytes(this string str)
        {
            const string hex_lookup = "0123456789ABCDEF";

            if (str.Length % 2 != 0)
                throw new FormatException($"Input string length must be divisible evenly by two ({str.Length})");

            ReadOnlySpan<char> chars = str.AsSpan();
            Span<byte> bytes = new byte[chars.Length / 2];

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
    }
}
