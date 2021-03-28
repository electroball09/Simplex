using System;
using System.Collections.Generic;
using System.Text;
using Simplex.User;
using System.Text.Json.Serialization;

namespace Simplex.Protocol
{
    public class UserDataRequest
    {
        public object _item { get; set; }
    }

    public class UserDataRequest<T> : UserDataRequest where T : UserData
    {
        [JsonIgnore]
        public T Item { get { return _item as T; } set { _item = value; } }
    }

    public class UserDataResponse
    {
        public object _item { get; set; }
    }

    public class UserDataResponse<T> : UserDataResponse where T : UserData
    {
        [JsonIgnore]
        public T Item { get { return _item as T; } set { _item = value; } }
    }
}
