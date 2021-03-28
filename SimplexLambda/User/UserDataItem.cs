using System;
using System.Collections.Generic;
using System.Text;
using Amazon.DynamoDBv2.DataModel;

namespace SimplexLambda.User
{
    public abstract class UserDataItem
    {
        [DynamoDBHashKey("Hash")]
        public Guid GUID { get; set; }
        [DynamoDBRangeKey]
        public string Range 
        { 
            get { return GetRange(); }
            set { }
        }

        protected abstract string GetRange();
    }

    public class UserDataItem<T> : UserDataItem
    {
        public T Item { get; set; }

        protected override string GetRange()
        {
            return GetType().Name;
        }
    }
}
