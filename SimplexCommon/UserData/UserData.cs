using System;
using System.Collections.Generic;
using System.Text;

namespace Simplex.User
{
    public abstract class UserData
    {
        public Guid GUID { get; set; }

        private string _type;
        public string Type
        {
            get
            {
                if (string.IsNullOrEmpty(_type))
                    _type = GetType().ToString();
                return _type;
            }
            set { _type = value; }
        }

        public abstract object Load(IUserDataLoader loader);
    }
}
