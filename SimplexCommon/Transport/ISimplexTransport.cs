using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Simplex;
using Simplex.Protocol;

namespace Simplex.Transport
{
    public interface ISimplexTransport
    {
        ISimplexLogger Logger { get; set; }

        void Initialize();
        Task<SimplexResponse<T>> SendRequest<T>(SimplexRequest rq) where T : class;
    }
}
