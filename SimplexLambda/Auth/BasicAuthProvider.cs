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
        public override SimplexError AuthUser(AuthRequest rq, AuthAccount acc, SimplexRequestContext context, out SimplexError err)
        {
            var diag = context.DiagInfo.BeginDiag("BASIC_USER_AUTH");

            string hash = LambdaUtil.HashInput(rq.AuthSecret, acc.Salt);

            if (hash != acc.Secret)
                err = SimplexError.GetError(SimplexErrorCode.InvalidAuthCredentials, "Passwords do not match");
            else
                err = SimplexError.OK;

            context.DiagInfo.EndDiag(diag);

            return err;
        }
    }
}
