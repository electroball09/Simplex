using System;
using System.Collections.Generic;
using System.Text;

namespace Simplex.Protocol
{
    public enum AuthType
    {
        Basic = 0,
        Google = 1,
        Steam = 2
    }

    public class AuthRequest
    {
        public AuthType AuthType { get; set; }
        public string AuthID { get; set; } = "";
        public string AuthSecret { get; set; } = "";
    }

    public class AccessCredentials
    {
        public Guid UserGUID { get; set; }
        public string AuthToken { get; set; }
    }
}
