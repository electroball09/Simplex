using System;
using System.Collections.Generic;
using System.Text;
using Amazon.DynamoDBv2.DataModel;
using Simplex.User;

namespace SimplexLambda.User
{
    public class UserAccessToken : DBUserDataItem<UserAccessToken>
    {
        [DynamoDBProperty]
        private string _token { get; set; }
        [DynamoDBProperty]
        private string _created { get; set; }
        [DynamoDBProperty]
        private string _lastAccessed { get; set; }
        [DynamoDBProperty]
        private string _timeout { get; set; }
        [DynamoDBProperty]
        private string _duration { get; set; }

        [DynamoDBIgnore]
        public DateTime Created
        {
            get { return DateTime.Parse(_created); }
            set { _created = value.ToString(); }
        }
        [DynamoDBIgnore]
        public DateTime LastAccessed
        {
            get { return DateTime.Parse(_lastAccessed); }
            set { _lastAccessed = value.ToString(); }
        }
        [DynamoDBIgnore]
        public TimeSpan Timeout
        {
            get { return TimeSpan.Parse(_timeout); }
            set { _timeout = value.ToString(); }
        }
        [DynamoDBIgnore]
        public TimeSpan Duration
        {
            get { return TimeSpan.Parse(_duration); }
            set { _duration = value.ToString(); }
        }
    }
}
