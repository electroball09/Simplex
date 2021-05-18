using System;
using System.Collections.Generic;
using System.Text;
using Simplex.User;
using System.Text.Json.Serialization;

namespace Simplex.Protocol
{
    public enum UserDataRequestType
    {
        _invalid = 0,
        GetUserData = 1,
        SetUserData = 2,
        UpdateUserData = 3
    }

    public class UserData
    {
        public string _dataType { get; set; }
        private object __item;
        public object _item
        { 
            get
            {
                return __item;
            }
            set
            {
                __item = value;
                _dataType = value.GetType().FullName;
            }
        }
    }

    public class UserDataResult : UserData
    {
        public SimplexError Error { get; set; }

        public UserDataResult(UserData data) => _item = data;
    }

    public class UserDataRequest
    {
        public Guid UserGUID { get; set; }
        public UserDataRequestType RequestType { get; set; }
        public List<UserData> UserDataList { get; set; } = new List<UserData>();
    }

    public class UserDataResponse
    {
        public List<UserDataResult> Results { get; set; } = new List<UserDataResult>();
    }
}
