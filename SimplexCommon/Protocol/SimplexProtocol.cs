#pragma warning disable CS0618

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Simplex.Protocol
{
    public enum SimplexRequestType
    {
        None = 0,

        PingPong,
        BatchRequest,

        GetServiceConfig,

        Auth,
        OAuth,

        UserData,
    }

    public class SimplexRequest
    {
        [JsonIgnore]
        public static int RequestIDCounter { get; private set; }

        public int RequestID { get; set; }
        public virtual SimplexRequestType RequestType { get; set; }
        private object _payload = null;
        public object Payload 
        {
            get => _payload;
            set => _payload = value;
        }
        public string AccessToken { get; set; } = null;
        public string ClientID { get; set; } = "";

        [JsonIgnore]
        public DateTime CreatedTime { get; }

        [Obsolete("don't use this")]
        protected SimplexRequest() { }

        public SimplexRequest(SimplexRequestType rqType, object payload)
        {
            RequestID = RequestIDCounter++;
            CreatedTime = DateTime.Now;
            RequestType = rqType;
            Payload = payload;
        }

        public SimplexError PayloadAs<T>(out T obj, out SimplexError err) where T : class
        {
            try
            {
                obj = JsonSerializer.Deserialize<T>(((JsonElement)Payload).GetRawText());

                err = SimplexErrorCode.OK;
            }
            catch (Exception ex)
            {
                obj = null;
                err = SimplexError.Custom(SimplexErrorCode.InvalidPayloadType, ex.ToString());
            }
            return err;
        }
    }

    public class SimplexResponse
    {
        public int RequestID { get; set; }
        public SimplexRequestType RequestType { get; set; }
        public SimplexError Error { get; set; }

        public string PayloadType { get; set; } = "";
        private object _payload = null;
        public object Payload 
        {
            get { return _payload; }
            set
            {
                if (value == null)
                    PayloadType = "";
                else
                    PayloadType = value.GetType().Name;
                _payload = value;
            }
        }

        public SimplexDiagnostics DiagInfo { get; set; }
        public List<object> Logs { get; set; } = new List<object>();

        [JsonIgnore]
        public TimeSpan TimeTaken { get; }
        [JsonIgnore]
        public ISimplexLogger Logger { get; set; }

        [Obsolete("don't use this")]
        protected SimplexResponse() { }

        public SimplexResponse(SimplexRequest rq)
        {
            TimeTaken = DateTime.Now - rq.CreatedTime;
            RequestID = rq.RequestID;
            RequestType = rq.RequestType;
        }

        public SimplexResponse(SimplexRequest rq, SimplexError err) : this(rq)
        {
            Error = err;
        }

        public T PayloadAs<T>() where T : class
        {
            try
            {
                if (Payload == null)
                    return null;

                if (PayloadType == "JsonElement")
                {
                    return JsonSerializer.Deserialize<T>(((JsonElement)Payload).GetRawText());
                }

                return Payload as T;
            }
            catch (Exception ex)
            {
                Console.WriteLine($">>>>>>>>>>>>>>>>>>>> {ex}");
                Logger?.Error(ex.Message);
                return null;
            }
        }
    }

    public class SimplexResponse<T> : SimplexResponse where T : class
    {
        [JsonIgnore]
        public T Data 
        { 
            get
            {
                T val = PayloadAs<T>();
                if (val == null)
                    throw new InvalidOperationException($"Payload [{Payload} - {Payload?.GetType()}] was null or not of the expected type");
                return val;
            }
        }
    }
}
