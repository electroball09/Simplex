using System;
using System.Collections.Generic;
using System.Text;
using Simplex.Protocol;
using SimplexLambda.DBSchema;

namespace SimplexLambda.Auth
{
    internal static class AuthUtil
    {
        static readonly Dictionary<AuthType, string> prefixes = new Dictionary<AuthType, string>()
        {
            { AuthType.Basic, "SIMPLEX" },
            { AuthType.Google, "GOOGLE" },
            { AuthType.Steam, "STEAM" }
        };
    }
}
