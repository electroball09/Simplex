#pragma warning disable CS0618

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

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
                err = SimplexError.Custom(SimplexErrorCode.InvalidRequestPayloadType, ex.ToString());
            }
            return err;
        }
    }

    public class SimplexResult
    {
        public SimplexError Error { get; set; }
        public string ResultJSON { get; set; }
        public string ResultType { get; set; }

        public static SimplexResult OK(object resultValue)
        {
            if (resultValue == null)
                throw new ArgumentNullException("resultValue");

            SimplexResult result = new SimplexResult
            {
                Error = SimplexErrorCode.OK,
            };
            result.SetResult(resultValue);

            return result;
        }

        public static SimplexResult Err(SimplexError error)
        {
            if (error == null)
                throw new ArgumentNullException("error");

            SimplexResult result = new SimplexResult
            {
                Error = error,
            };

            result.SetResult(null);

            return result;
        }

        public static SimplexResult Copy(SimplexResult orig)
        {
            return new SimplexResult().CopyFrom(orig);
        }

        private void SetResult(object result)
        {
            if (result == null)
            {
                ResultJSON = "";
                ResultType = "";
                return;
            }

            ResultJSON = JsonSerializer.Serialize(result, result.GetType());
            ResultType = result.GetType().AssemblyQualifiedName;
        }

        [Obsolete("dont use this")]
        public SimplexResult() { }

        public void Get<T>(Action<SimplexError> OnErr, Action<T> OnSome, Action OnOtherType = null)
        {
            if (Error.Code != SimplexErrorCode.OK)
            {
                OnErr(Error);
            }
            else
            {
                if (ResultType == typeof(T).AssemblyQualifiedName)
                {
                    OnSome(JsonSerializer.Deserialize<T>(ResultJSON));
                }
                else
                {
                    if (OnOtherType == null)
                    {
                        Console.WriteLine("Other type was encountered and not handled!");
                        Console.WriteLine(Environment.StackTrace);
                    }
                    else
                        OnOtherType();
                }
            }
        }

        public async Task GetAsync<T>(Func<SimplexError, Task> OnErr, Func<T, Task> OnSome, Func<Task> OnOtherType = null)
        {
            if (Error.Code != SimplexErrorCode.OK)
            {
                await OnErr(Error);
            }
            else
            {
                if (ResultType == typeof(T).AssemblyQualifiedName)
                {
                    await OnSome(JsonSerializer.Deserialize<T>(ResultJSON));
                }
                else
                {
                    if (OnOtherType == null)
                    {
                        Console.WriteLine("Other type was encountered and not handled!");
                        Console.WriteLine(Environment.StackTrace);
                    }
                    else
                        await OnOtherType();
                }
            }
        }

        public async Task GetAsyncSome<T>(Action<SimplexError> OnErr, Func<T, Task> OnSome, Func<Task> OnOtherType = null)
        {
            if (Error.Code != SimplexErrorCode.OK)
            {
                OnErr(Error);
            }
            else
            {
                if (ResultType == typeof(T).AssemblyQualifiedName)
                {
                    await OnSome(JsonSerializer.Deserialize<T>(ResultJSON));
                }
                else
                {
                    if (OnOtherType == null)
                    {
                        Console.WriteLine("Other type was encountered and not handled!");
                        Console.WriteLine(Environment.StackTrace);
                    }
                    else
                        await OnOtherType();
                }
            }
        }

        public SimplexResult Sub<TResult, TNew>(Func<TResult, TNew> OnSome)
        {
            SimplexResult result = null;
            Get<TResult>((err) => result = SimplexResult.Err(err),
                (obj) => result = SimplexResult.OK(OnSome(obj)),
                () => result = Copy(this));
            return result;
        }

        public SimplexResult CopyFrom(SimplexResult result)
        {
            Error = result.Error;
            ResultJSON = result.ResultJSON;
            ResultType = result.ResultType;
            return this;
        }

        public SimplexResult<T> To<T>()
        {
            return new SimplexResult<T>(this);
        }
    }

    public class SimplexResult<T> : SimplexResult
    {
        public void Get(Action<SimplexError> OnErr, Action<T> OnSome, Action OnOtherType = null) => Get<T>(OnErr, OnSome, OnOtherType);
        public async Task GetAsync(Func<SimplexError, Task> OnErr, Func<T, Task> OnSome, Func<Task> OnOtherType = null) => await GetAsync<T>(OnErr, OnSome, OnOtherType);
        public async Task GetAsyncSome(Action<SimplexError> OnErr, Func<T, Task> OnSome, Func<Task> OnOtherType = null) => await GetAsyncSome<T>(OnErr, OnSome, OnOtherType);
        public SimplexResult Sub<TNew>(Func<T, TNew> OnSome) => Sub<T, TNew>(OnSome);

        internal SimplexResult(SimplexResult old)
        {
            CopyFrom(old);
        }
    }

    public class SimplexLambdaResponse : SimplexResponse
    {
        public SimplexLambdaResponse(SimplexRequest rq, SimplexResult result) : base(rq)
        {
            Result = result;
        }
    }

    public class SimplexResponse
    {
        public int RequestID { get; set; }
        public SimplexRequestType RequestType { get; set; }

        public SimplexResult Result { get; set; }

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
    }
}
