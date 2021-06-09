using System;
using System.Collections.Generic;
using System.Text;
using Simplex.Util;
using System.Text.Json.Serialization;

namespace Simplex.Protocol
{
    public class AuthRequest
    {
        public virtual AuthServiceIdentifier ServiceIdentifier { get; set; }
        public string AccountID { get; set; }
        public string AccountSecret { get; set; } = "";
        public bool CreateAccountIfNonexistent { get; set; } = false;

        [JsonIgnore]
        public virtual DateTime OverrideAuthExpiryUTC { get => DateTime.MinValue; }
    }
    
    public class AuthResponse
    {
        public AccessCredentials Credentials { get; set; }
        public AuthAccountDetails AccountDetails { get; set; }
    }

    public class AuthAccountDetails
    {
        public AuthServiceIdentifier ServiceIdentifier { get; set; }
        public string AccountID { get; set; }
        public string EmailAddress { get; set; }
    }

    public class AccessCredentials
    {
        public Guid UserGUID { get; set; }
        public string AuthToken { get; set; }
    }
}
