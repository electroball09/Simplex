using System;
using System.Collections.Generic;
using System.Text;
using Simplex.Protocol;

namespace Simplex.UserData
{
    public class UserBasicDataPublic
    {
        public DateTime CreationDate { get; set; }
    }

    public class UserBasicDataPrivate
    {
        public List<AuthAccountDetails> ConnectedAccounts { get; set; } = new List<AuthAccountDetails>();
    }
}
