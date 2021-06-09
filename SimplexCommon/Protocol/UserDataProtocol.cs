using System;
using System.Collections.Generic;
using System.Text;
using Simplex.UserData;
using System.Text.Json.Serialization;
using System.Text.Json;

namespace Simplex.Protocol
{
    public enum UserDataRequestType
    {
        GetUserData = 1,
        SetUserData = 2,
        UpdateUserData = 3
    }

    public class UserDataRequest
    {
        public UserDataRequestType RequestType { get; set; }
        public Guid UserGUID { get; set; }
        public List<UserDataOperation> UserDataList { get; set; } = new List<UserDataOperation>();
    }

    public class UserDataResponse
    {
        public Guid UserGUID { get; set; }
        public List<UserDataResult> Results { get; set; } = new List<UserDataResult>();
    }

    public abstract class UserDataBase
    {
        public string __dataType { get; set; }
        public string __dataJson { get; set; }

        public UserDataBase() { }

        public void SetDataJSON<T>(T obj)
        {
            __dataType = obj.GetType().Name;
            __dataJson = JsonSerializer.Serialize<T>(obj);
        }

        public T GetData<T>()
        {
            return JsonSerializer.Deserialize<T>(__dataJson);
        }
    }

    public class UserDataOperation : UserDataBase
    {
        public string CustomDataID { get; set; } = "";

        internal string GetDBRange()
        {
            return __dataType + (string.IsNullOrEmpty(CustomDataID) ? "" : $"_{CustomDataID}");
        }
    }

    public class UserDataResult : UserDataBase
    {
        public SimplexError Error { get; set; }

        public UserDataResult(UserDataOperation data)
        {
            __dataType = data.__dataType;
            __dataJson = data.__dataJson;
        }

        [Obsolete("dont use this")]
        public UserDataResult() { }
    }
}
