using System;
using System.Collections.Generic;
using System.Text;
using Amazon.DynamoDBv2.DataModel;
using System.Text.RegularExpressions;
using Simplex.Protocol;

namespace SimplexLambda.DBSchema
{
    public class AuthAccount
    {
        public static AuthAccount Create(AuthType authType, string id)
        {
#pragma warning disable CS0618
            return new AuthAccount()
            {
                AccountID = id,
                AccountPrefix = authType.ToString().ToUpper()
            };
#pragma warning restore CS0618
        }

        [DynamoDBHashKey]
        public string Hash { get; set; } = "ACC<accid>";
        [DynamoDBRangeKey]
        public string Range { get; set; } = "AuthAccount";

        [DynamoDBIgnore]
        public string AccountID
        {
            get
            {
                return Regex.Match(Hash, "<([^<>]+)>").Groups[1].Value;
            }
            set
            {
                Hash = Regex.Replace(Hash, "<([^<>]+)>", $"<{value}>");
            }
        }

        [DynamoDBIgnore]
        public string AccountPrefix
        {
            get
            {
                return Regex.Match(Hash, "([^<>]+)<").Groups[1].Value;
            }
            set
            {
                Hash = Regex.Replace(Hash, "([^<>]+)<", $"{value}<");
            }
        }

        [Obsolete("Use AuthAccount.Create()")]
        public AuthAccount() { }

        public string Secret { get; set; }
        public string Salt { get; set; }
        public Guid ConnectedUserGUID { get; set; }
    }
}
