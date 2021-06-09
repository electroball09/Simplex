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
        static Dictionary<AuthServiceIdentifier.AuthType, Func<AuthProvider>> providerMap = new Dictionary<AuthServiceIdentifier.AuthType, Func<AuthProvider>>()
        {
            { AuthServiceIdentifier.AuthType.Basic, () => new BasicAuthProvider() },
            { AuthServiceIdentifier.AuthType.Google, () => new OAuthProvider() },
            { AuthServiceIdentifier.AuthType.Twitch, () => new OAuthProvider() },
        };

        public static SimplexError GetProvider(AuthServiceIdentifier identifier, out AuthProvider provider, out SimplexError err)
        {
            if (providerMap.TryGetValue(identifier.Type, out var func))
            {
                provider = func();
                err = SimplexErrorCode.OK;
            }
            else
            {
                provider = null;
                err = SimplexError.Custom(SimplexErrorCode.LambdaMisconfiguration, $"Auth service was not found for identifier {identifier}");
            }
            return err;
        }

        protected static SimplexError LoadAccount(AuthRequest rq, AuthServiceParamsLambda authParams, SimplexRequestContext context, out AuthAccount acc, out SimplexError err)
        {
            var tmpAcc = AuthAccount.Create(authParams.Identifier, rq.AccountID);
            tmpAcc.Secret = rq.AccountSecret;

            if (!context.DB.LoadItem(tmpAcc, out acc, context, out err))
            {
                
            }
            err.Substitute(SimplexErrorCode.DBItemNonexistent, SimplexErrorCode.AuthAccountNonexistent);

            return err;
        }
        
        public abstract SimplexError AuthUser(AuthServiceParamsLambda authParams, SimplexRequestContext context, out AuthRequest authRq, out AuthAccount acc, out SimplexError err);
    }
}
