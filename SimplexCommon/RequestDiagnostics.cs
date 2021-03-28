using System;
using System.Collections.Generic;
using System.Text;

namespace Simplex
{
    public class RequestDiagnostics
    {
        public class DiagInfo
        {
            public string DiagID { get; set; }
            public double TimeTakenMS { get; set; }

            DiagInfo() { }

            public DiagInfo(string id, TimeSpan time)
            {
                DiagID = id;
                TimeTakenMS = time.TotalMilliseconds;
            }
        }
        Dictionary<string, DateTime> diags = new Dictionary<string, DateTime>();

        public List<DiagInfo> DiagInfos { get; set; } = new List<DiagInfo>();

        public RequestDiagnostics() { }

        public void BeginDiag(string id)
        {
            if (diags.ContainsKey(id))
                return;

            diags.Add(id, DateTime.Now);
        }

        public void EndDiag(string id)
        {
            if (!diags.ContainsKey(id))
                return;

            DiagInfos.Add(new DiagInfo(id, DateTime.Now - diags[id]));
            diags.Remove(id);
        }

        public void DebugLog(ISimplexLogger log)
        {
            log.Debug("-Request diag info");
            foreach (var d in DiagInfos)
                log.Debug($"  {d.DiagID} - {d.TimeTakenMS} ms");
        }
    }
}
