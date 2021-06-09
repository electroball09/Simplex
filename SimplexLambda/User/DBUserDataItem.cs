using System;
using System.Collections.Generic;
using System.Text;
using Amazon.DynamoDBv2.DataModel;

namespace SimplexLambda.User
{
    public class DBUserDataItem
    {
        [DynamoDBHashKey("Hash")]
        public Guid GUID { get; set; }
        [DynamoDBRangeKey]
        public string Range 
        { 
            get { return GetRange(); }
            set { }
        }

        protected virtual string GetRange() { return null; }
    }

    public class DBUserDataItem<T> : DBUserDataItem
    {
        public T Item { get; set; }

        protected override string GetRange()
        {
            return GetType().Name;
        }
    }
}
