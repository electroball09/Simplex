using System;
using System.Collections.Generic;
using System.Text;

namespace Simplex.User
{
    public interface IUserDataLoader
    {
        T LoadData<T>(Guid guid, string token) where T : UserData;
    }
}
