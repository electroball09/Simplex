using System;
using System.Collections.Generic;
using System.Text;
using Simplex;
using Amazon.DynamoDBv2.DataModel;
using Simplex.Protocol;
using SimplexLambda.DBSchema;
using System.Security.Cryptography;

namespace SimplexLambda.Auth
{
    public abstract class AuthProvider
    {
        protected static SHA256 SHA { get; } = SHA256.Create();

        static Dictionary<AuthType, Type> providerMap = new Dictionary<AuthType, Type>()
        {
            { AuthType.Basic, typeof(BasicAuthProvider) },
        };

        public static AuthProvider GetProvider(AuthType authType)
        {
            return (AuthProvider)Activator.CreateInstance(providerMap[authType]);
        }

        public abstract SimplexError AuthUser(AuthRequest rq, AuthAccount acc, SimplexRequestContext context, out SimplexError err);
    }
}
