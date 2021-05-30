#pragma warning disable CS0618

using System;
using System.Collections.Generic;
using System.Text;

namespace Simplex.Protocol
{
    public class SimplexBatchRequest : SimplexRequest
    {
        public override SimplexRequestType RequestType { get => SimplexRequestType.BatchRequest; set { } }

        public List<SimplexRequest> Requests { get; set; } = new List<SimplexRequest>();
    }

    public class SimplexBatchResponse : SimplexResponse<List<SimplexResponse>>
    {
        public List<SimplexResponse> Responses => Data;
    }
}
