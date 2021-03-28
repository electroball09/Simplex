using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace Simplex
{
    public enum SimplexErrorCode : ushort
    {
        OK = 0xFFFF,
        Unknown = 0x0000,

        //common errors
        InvalidCryptographyConfiguration,

        //client errors
        InvalidResponsePayloadType,

        // server -> client error
        LambdaMisconfiguration,
        ResponseMalformed,
        InvalidRequestType,
        AccessTokenInvalid,
        InvalidAuthCredentials,

        //server errors
        DBItemNonexistent,
        AuthAccountNonexistent,
    }

    public class SimplexError
    {
        static Dictionary<SimplexErrorCode, string> friendlyStrings = new Dictionary<SimplexErrorCode, string>()
        {
            { SimplexErrorCode.Unknown, "An unknown error occured" },
            { SimplexErrorCode.OK, "OK" },
            { SimplexErrorCode.InvalidRequestType, "Invalid request type" },
            { SimplexErrorCode.DBItemNonexistent, "" },
            { SimplexErrorCode.LambdaMisconfiguration, "An invalid configuration of the lambda was detected" },
            { SimplexErrorCode.InvalidResponsePayloadType, "Response payload was of the wrong type" },
            { SimplexErrorCode.AuthAccountNonexistent, "No auth account with the specified type, id, and secret could be found" },
            { SimplexErrorCode.AccessTokenInvalid, "Access token is either expired or nonexistent" },
            { SimplexErrorCode.ResponseMalformed, "Server error: response malformed" },
            { SimplexErrorCode.InvalidCryptographyConfiguration, "The requested cryptography function returned an error" },
            { SimplexErrorCode.InvalidAuthCredentials, "The provided auth credentials are invalid" },
        };

        private static readonly SimplexError _ok = GetError(SimplexErrorCode.OK);
        [JsonIgnore]
        public static SimplexError OK => _ok;

        public static List<string> ValidateErrors()
        {
            var a = Enum.GetNames(typeof(SimplexErrorCode));
            List<string> namesNotFound = new List<string>();
            foreach (var b in a)
            {
                if (!friendlyStrings.ContainsKey(Enum.Parse<SimplexErrorCode>(b)))
                    namesNotFound.Add(b);
            }
            return namesNotFound;
        }

        public static SimplexError GetError(SimplexErrorCode err, string customMsg = null)
        {
            return new SimplexError(err, customMsg);
        }

        [Obsolete("don't use this")]
        SimplexError() { }

        SimplexError(SimplexErrorCode err, string customMsg)
        {
#pragma warning disable CS0618
            Code = err;
            Message = customMsg != null ? customMsg : friendlyStrings[err];
#pragma warning restore CS0618
        }

        public SimplexErrorCode Code { get; [Obsolete("don't use this")] set; }
        public string Message { get; [Obsolete("don't use this")] set; }

        public override bool Equals(object obj)
        {
            if (obj is SimplexError)
                return (obj as SimplexError).Code == this.Code;
            return false;
        }

        public static bool operator ==(SimplexError a, SimplexError b)
        {
            return a?.Code == b?.Code;
        }

        public static bool operator !=(SimplexError a, SimplexError b)
        {
            return !(a == b);
        }

        public static implicit operator bool(SimplexError e)
        {
            return e.Code != SimplexErrorCode.OK;
        }

        public override string ToString()
        {
            return $"{Code} - {Message}";
        }

        public override int GetHashCode()
        {
            return (int)Code;
        }

        public void Throw()
        {
            throw new Exception(ToString());
        }
    }
}
