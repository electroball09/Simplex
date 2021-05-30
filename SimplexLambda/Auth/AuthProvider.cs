using System;
using System.Collections.Generic;
using System.Text;
using Simplex;
using Amazon.DynamoDBv2.DataModel;
using Simplex.Protocol;
using SimplexLambda.DBSchema;
using System.Security.Cryptography;
using SimplexLambda.User;

namespace SimplexLambda.Auth
{
    public abstract class AuthProvider
    {
        static Dictionary<AuthType, Func<AuthProvider>> providerMap = new Dictionary<AuthType, Func<AuthProvider>>()
        {
            { AuthType.Basic, () => new BasicAuthProvider() },
        };

        public static AuthProvider GetProvider(AuthType authType)
        {
            if (authType.HasFlag(AuthType.IsOAuth))
                return new OAuthProvider();
            if (providerMap.TryGetValue(authType, out var func))
                return func();
            return null;
        }

        protected static SimplexError LoadAccount(AuthRequest rq, AuthServiceParamsLambda authParams, SimplexRequestContext context, out AuthAccount acc, out SimplexError err)
        {
            acc = AuthAccount.Create(authParams, rq.AccountID);

            if (!context.DB.LoadItem(acc, out acc, context, out err))
            {
                if (err.Code == SimplexErrorCode.DBItemNonexistent)
                    err = SimplexError.GetError(SimplexErrorCode.AuthAccountNonexistent);
                acc = null;

                return err;
            }

            return SimplexError.OK;
        }
        
        public abstract SimplexError AuthUser(AuthServiceParamsLambda authParams, SimplexRequestContext context, out AuthAccount acc, out SimplexError err);
    }
}
