using System;
using System.Collections.Generic;
using System.Text;

namespace Simplex.User
{
    public class UserCreationData : UserData
    {
        public DateTime CreationDate { get; set; }

        public override object Load(IUserDataLoader loader)
        {
            return null;// loader.LoadData<UserCreationData>();
        }
    }
}
