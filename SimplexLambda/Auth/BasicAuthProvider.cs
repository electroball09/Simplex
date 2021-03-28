using Amazon.DynamoDBv2.DataModel;
using Simplex;
using System;
using System.Collections.Generic;
using System.Text;
using Simplex.Protocol;
using SimplexLambda.DBSchema;
using System.Security.Cryptography;

namespace SimplexLambda.Auth
{
    public class BasicAuthProvider : AuthProvider
    {
        public override SimplexError AuthUser(AuthRequest rq, AuthAccount acc, SimplexRequestContext context)
        {
            context.DiagInfo.BeginDiag("BASIC_USER_AUTH");

            string hash = LambdaUtil.HashInput(rq.AuthSecret, acc.Salt);

            context.DiagInfo.EndDiag("BASIC_USER_AUTH");

            if (hash != acc.Secret)
                return SimplexError.GetError(SimplexErrorCode.InvalidAuthCredentials, "Passwords do not match");

            return SimplexError.OK;
        }
    }
}
