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

    public class ArbitraryTypedJSON
    {
        public string DataType { get; set; }
        public string DataJSON { get; set; }

        private Type _type;
        [JsonIgnore]
        public Type Type
        {
            get
            {
                if (string.IsNullOrEmpty(DataType))
                    throw new InvalidOperationException("Trying to get type of an arbitrary JSON that has no type!");

                if (_type == null)
                    _type = Type.GetType(DataType);

                return _type;
            }
        }

        public ArbitraryTypedJSON() { }
        public ArbitraryTypedJSON(object obj) => SetData(obj);

        public void SetData(object obj)
        {
            Type type = obj.GetType();
            DataType = type.AssemblyQualifiedName;
            DataJSON = JsonSerializer.Serialize(obj, type);
        }

        public bool IsType<T>()
        {
            return this.Type == typeof(T);
        }
    }

    public abstract class UserDataBase
    {
        public ArbitraryTypedJSON Json { get; set; }
        public object Object { set => Json = new ArbitraryTypedJSON(value); }

        public UserDataBase() { }
    }

    public class UserDataOperation : UserDataBase
    {
        public string CustomDataID { get; set; } = "";

        public UserDataOperation() : base() { }
        public UserDataOperation(object obj) => Json = new ArbitraryTypedJSON(obj);
        public UserDataOperation(object obj, string customDataId) : this(obj) => CustomDataID = customDataId;

        internal string GetDBRange()
        {
            return Json.Type.FullName + (string.IsNullOrEmpty(CustomDataID) ? "" : $"_{CustomDataID}");
        }
    }

    public class UserDataResult : UserDataBase
    {
        public SimplexError Error { get; set; }

        public UserDataResult(UserDataOperation data)
        {
            Json = data.Json;
        }

        public void Get<T>(Action<SimplexError> OnErr, Action<T> OnSome)
        {
            if (!Error)
                OnErr(Error);
            else
            {
                if (Json.IsType<T>())
                    OnSome(JsonSerializer.Deserialize<T>(Json.DataJSON));
                else
                    throw new InvalidOperationException($"Type {typeof(T)} did not match result type {Json.DataType}");
            }
        }

        [Obsolete("dont use this")]
        public UserDataResult() { }
    }
}
