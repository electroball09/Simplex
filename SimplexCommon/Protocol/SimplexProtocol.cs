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

        UserData,
    }

    public class SimplexRequest
    {
        [JsonIgnore]
        public static int RequestIDCounter { get; private set; }

        public int RequestID { get; set; }
        public virtual SimplexRequestType RequestType { get; set; }
        public object Payload { get; set; } = null;
        public string AccessToken { get; set; } = null;

        [JsonIgnore]
        public DateTime RequestedTime { get; }

        [Obsolete("don't use this")]
        protected SimplexRequest() { }

        public SimplexRequest(SimplexRequestType rqType, object payload)
        {
            RequestID = RequestIDCounter++;
            RequestedTime = DateTime.Now;
            RequestType = rqType;
            Payload = payload;
        }

        public T PayloadAs<T>() where T : class
        {
            try
            {
                if (!(Payload is T))
                    Payload = JsonSerializer.Deserialize<T>(((JsonElement)Payload).GetRawText());

                return Payload as T;
            }
            catch (Exception ex)
            {
                return null;
            }
        }
    }

    public class SimplexBatchRequest : SimplexRequest
    {
        public override SimplexRequestType RequestType { get => SimplexRequestType.BatchRequest; set { } }

        public List<SimplexRequest> Requests { get; set; } = new List<SimplexRequest>();
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

        public RequestDiagnostics DiagInfo { get; set; }
        public List<object> Logs { get; set; } = new List<object>();

        [JsonIgnore]
        public TimeSpan TimeTaken { get; }
        [JsonIgnore]
        public ISimplexLogger Logger { get; set; }

        [Obsolete("don't use this")]
        protected SimplexResponse() { }

        public SimplexResponse(SimplexRequest rq)
        {
            TimeTaken = DateTime.Now - rq.RequestedTime;
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
                if (!(Payload is T))
                    Payload = JsonSerializer.Deserialize<T>(((JsonElement)Payload).GetRawText());

                return Payload as T;
            }
            catch (Exception ex)
            {
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
                    throw new InvalidOperationException("Payload was null or not of the expected type");
                return val;
            }
        }
    }

    public class SimplexBatchResponse : SimplexResponse<List<SimplexResponse>>
    {
        public List<SimplexResponse> Responses => Data;
    }
}
