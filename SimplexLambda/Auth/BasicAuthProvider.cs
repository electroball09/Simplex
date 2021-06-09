using Amazon.DynamoDBv2.DataModel;
using Simplex;
using System;
using System.Collections.Generic;
using System.Text;
using Simplex.Protocol;
using SimplexLambda.DBSchema;
using System.Security.Cryptography;
using SimplexLambda.User;
using Simplex.Util;

namespace SimplexLambda.Auth
{
    public class BasicAuthProvider : AuthProvider
    {
        public override SimplexError AuthUser(AuthServiceParamsLambda authParams, SimplexRequestContext context, out AuthRequest authRq, out AuthAccount acc, out SimplexError err)
        {
            var diag = context.DiagInfo.BeginDiag("BASIC_USER_AUTH");

            acc = null; // PLS PAY ATTENTION TO THIS PLS PLS PLS

            SimplexError EndRequest(SimplexError error)
            {
                context.DiagInfo.EndDiag(diag);
                return error;
            }

            if (!context.Request.PayloadAs<AuthRequest>(out authRq, out err))
            {
                return EndRequest(err);
            }

            if (!LoadAccount(authRq, authParams, context, out acc, out err))
            {
                return EndRequest(err);
            }

            if (!SimplexUtil.DecryptString(context.LambdaConfig.PrivateRSA, authRq.AccountSecret, out string decryptedSecret, out var decryptError))
            {
                err = SimplexErrorCode.InvalidAuthCredentials;
                return EndRequest(err);
            }

            authRq.AccountSecret = decryptedSecret;
            string hash = LambdaUtil.HashInput(context.SHA, authRq.AccountSecret, acc.Salt);

            if (hash != acc.Secret)
                err = SimplexError.Custom(SimplexErrorCode.InvalidAuthCredentials, "tmp pw mismatch");
            else
                err = SimplexErrorCode.OK;

            return EndRequest(err);
        }
    }
}
