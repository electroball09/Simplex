﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Simplex.Protocol;
using Simplex.Transport;
using RestSharp;
using RestSharp.Serializers.SystemTextJson;
using System.Text.Json;
using Flurl;

namespace Simplex
{
    public class SimplexRestTransportConfig
    {
        public static SimplexRestTransportConfig Copy(SimplexRestTransportConfig old)
        {
            SimplexRestTransportConfig copy = new SimplexRestTransportConfig()
            {
                APIUrl = old.APIUrl,
                APIStage = old.APIStage,
                APIResource = old.APIResource,
            };

            return copy;
        }

        public string APIUrl { get; set; } = "";
        public string APIStage { get; set; } = "";
        public string APIResource { get; set; } = "";

        public string AssembleURL()
        {
            if (string.IsNullOrEmpty(APIUrl))
            {
                throw new InvalidOperationException("API URL is empty!");
            }

            return APIUrl
                .AppendPathSegment(APIStage)
                .AppendPathSegment(APIResource);
        }

        public SimplexRestTransportConfig Copy()
        {
            return Copy(this);
        }
    }

    public class SimplexRestTransport : ISimplexTransport
    {
        public ISimplexLogger Logger { get; set; }
        public SimplexRestTransportConfig Config { get; set; }

        RestClient client;

        public SimplexRestTransport()
        {
            
        }

        public void Initialize()
        {
            if (Config == null)
                throw new InvalidOperationException("Config must be set before transport can be initialized!");

            client = new RestClient(Config.AssembleURL());
            client.UseSerializer<SystemTextJsonSerializer>();
        }

        public async Task<SimplexResponse<T>> SendRequest<T>(SimplexRequest request) where T : class
        {
            return await Task.Run
                (async () =>
                {
                    Logger.Debug($"Sending request of type {request.RequestType}");

                    var json = await RequestGetJson(request);
                    Logger.Debug($"-request ID {request.RequestID} took {(DateTime.Now - request.RequestedTime).TotalMilliseconds} ms");
                    var rsp = JsonSerializer.Deserialize<SimplexResponse<T>>(json);
                    rsp.DiagInfo?.DebugLog(Logger);
                    if (!rsp.Error && !rsp.DeserializePayload())
                    {
                        Logger.Error($"Response type mismatch! Wanted type: {typeof(T).Name}  received type: {rsp.PayloadType}");
                        rsp.Payload = null;
                        rsp.Error = SimplexError.GetError(SimplexErrorCode.InvalidResponsePayloadType);
                    }
                    return rsp;
                }, new System.Threading.CancellationToken());
        }

        internal async Task<string> RequestGetJson(SimplexRequest request)
        {
            var rest = new RestRequest("", Method.POST, DataFormat.Json);
            string jsonStr = JsonSerializer.Serialize(request);
            Logger.Debug($"request - {jsonStr}");
            rest.AddJsonBody(request);
            var resp = await client.ExecuteAsync<SimplexResponse>(rest);
            if (resp == null)
                throw new InvalidOperationException("Server returned a null response!");
            Logger.Debug($"response - {resp.Content}");
            return resp.Content;
        }
    }
}