#pragma warning disable CS0618
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

        //lol
        WTF,

        //common errors
        InvalidCryptographyConfiguration,

        //client errors
        InvalidResponsePayloadType,

        // server -> client error
        LambdaMisconfiguration,
        InvalidRequestType,
        AccessTokenInvalid,
        InvalidAuthCredentials,
        InvalidPayloadType,
        AuthServiceDisabled,

        //access errors
        PermissionDenied,
        AccessTokenExpired,


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
            { SimplexErrorCode.WTF, "wtf u trying to do?" },
            { SimplexErrorCode.InvalidRequestType, "Invalid request type" },
            { SimplexErrorCode.DBItemNonexistent, "" },
            { SimplexErrorCode.LambdaMisconfiguration, "An invalid configuration of the lambda was detected" },
            { SimplexErrorCode.InvalidResponsePayloadType, "Response payload was of the wrong type" },
            { SimplexErrorCode.AuthAccountNonexistent, "No auth account with the specified type, id, and secret could be found" },
            { SimplexErrorCode.AccessTokenInvalid, "Access token is either expired or nonexistent" },
            { SimplexErrorCode.InvalidCryptographyConfiguration, "The requested cryptography function returned an error" },
            { SimplexErrorCode.InvalidAuthCredentials, "The provided auth credentials are invalid" },
            { SimplexErrorCode.InvalidPayloadType, "The provided payload was of an incorrect type" },
            { SimplexErrorCode.PermissionDenied, "Access to this resource was denied" },
            { SimplexErrorCode.AccessTokenExpired, "Access token has expired.  Please reauthenticate" },
            { SimplexErrorCode.AuthServiceDisabled, "The requested auth service is not enabled" },
        };

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

        public static SimplexError Custom(SimplexErrorCode err, string customMsg)
        {
            return new SimplexError(err, customMsg);
        }

        [Obsolete("don't use this")]
        SimplexError() { }

        SimplexError(SimplexErrorCode err, string customMsg)
        {
            Code = err;
            Message = customMsg != null ? customMsg : friendlyStrings[err];
        }

        public SimplexErrorCode Code { get; [Obsolete("don't use this")] set; }
        public string Message { get; [Obsolete("don't use this")] set; }

        public void Substitute(SimplexErrorCode oldCode, SimplexErrorCode newCode)
        {
            if (Code == oldCode)
            {
                Code = newCode;
                Message = friendlyStrings[Code];
            }
        }

        public override bool Equals(object obj)
        {
            if (obj is SimplexError)
                return (obj as SimplexError).Code == this.Code;
            return false;
        }

        public static bool operator ==(SimplexError a, SimplexError b) => a?.Code == b?.Code;

        public static bool operator !=(SimplexError a, SimplexError b) => !(a == b);

        public static implicit operator bool(SimplexError e) => e?.Code == SimplexErrorCode.OK;

        public static implicit operator SimplexError(SimplexErrorCode code) => new SimplexError(code, null);

        public static implicit operator SimplexErrorCode(SimplexError err) => err.Code;

        public override string ToString() => $"{Code} - {Message}";

        public override int GetHashCode() => (int)Code;

        public void Throw()
        {
            throw new Exception(ToString());
        }
    }
}
