#pragma warning disable CS0618

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Simplex.Protocol
{
    public enum SimplexRequestType
    {
        None = 0,

        PingPong,

        GetServiceConfig,

        Auth,

        RequestUserData,
        UpdateUserData,
    }

    public class SimplexRequest
    {
        [JsonIgnore]
        public static int RequestIDCounter { get; private set; }

        public int RequestID { get; set; }
        public SimplexRequestType RequestType { get; set; }
        public object Payload { get; set; } = null;

        [JsonIgnore]
        public DateTime RequestedTime { get; }
        [JsonIgnore]
        public Action<SimplexResponse> Callback { get; set; }

        [Obsolete("don't use this")]
        protected SimplexRequest() { }

        public SimplexRequest(SimplexRequestType rqType, object payload)
        {
            RequestID = RequestIDCounter++;
            RequestedTime = DateTime.Now;
            RequestType = rqType;
            Payload = payload;
        }

        public SimplexRequest(SimplexRequestType rqType, object payload, Action<SimplexResponse> callback) : this(rqType, payload)
        {
            Callback = callback;
        }
    }

    public class SimplexRequest<T> : SimplexRequest where T : class
    {
        [JsonIgnore]
        public T Item { get { return Payload as T; } set { Payload = value; } }

        SimplexRequest() : base() { }
        public SimplexRequest(SimplexRequestType rqType, T item) : base(rqType, item) { }
    }

    public class SimplexResponse
    {
        public int RequestID { get; }
        public SimplexRequestType RequestType { get; }
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

        public RequestDiagnostics DiagInfo { get; set; }

        [JsonIgnore]
        public TimeSpan TimeTaken { get; }

        [Obsolete("don't use this")]
        protected SimplexResponse() { }

        public SimplexResponse(SimplexRequest rq)
        {
            TimeTaken = DateTime.Now - rq.RequestedTime;
            RequestID = rq.RequestID;
            RequestType = rq.RequestType;
        }
    }

    public class SimplexResponse<T> : SimplexResponse where T : class
    {
        [JsonIgnore]
        public T Item { get { return Payload as T; } set { Payload = value; } }

        SimplexResponse() : base() { }

        public SimplexResponse(SimplexRequest rq, T item) : base(rq)
        {
            Item = item;
        }

        public bool DeserializePayload()
        {
            if (Payload == null) return true;

            Payload = JsonSerializer.Deserialize<T>(((JsonElement)Payload).GetRawText());

            return Item != null;
        }
    }
}
