using System;
using System.Collections.Generic;
using System.Text;
using Simplex.Util;

namespace Simplex.Protocol
{
    public enum AuthType : byte
    {
        IsOAuth = 0b10000000,

         Basic = 0x01,
        Google = 0x02 | IsOAuth,
         Steam = 0x03,
        Twitch = 0x04 | IsOAuth,

           MAX = 0x80,
        INVALID = 0x00
    }

    public class AuthRequest
    {
        private AuthType _authType;
        public virtual AuthType AuthType 
        { 
            get
            {
                if (EnumValidator<AuthType>.IsValid(_authType))
                    return _authType;
                return AuthType.INVALID;
            }
            set
            {
                _authType = value;
            }
        }
        public string AccountID { get; set; }
        public string AccountSecret { get; set; } = "";
    }

    public class AuthResponse
    {
        public AuthType AuthType { get; set; }
        public AccessCredentials Credentials { get; set; }
        public AuthAccountDetails AccountDetails { get; set; }
    }

    public class AuthAccountDetails
    {
        public AuthType AuthType { get; set; }
        public string AccountID { get; set; }
    }

    public class AccessCredentials
    {
        public Guid UserGUID { get; set; }
        public string AuthToken { get; set; }
    }
}
