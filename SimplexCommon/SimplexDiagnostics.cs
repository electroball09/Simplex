using System;
using System.Collections.Generic;
using System.Text;

namespace Simplex
{
    public class SimplexDiagnostics
    {
        public struct DiagHandle
        {
            public string DiagID { get; set; }
            public double TimeTakenMS { get; set; }

            private DateTime StartTime { get; }

            public DiagHandle(string id)
            {
                DiagID = id;
                TimeTakenMS = 0;
                StartTime = DateTime.Now;
            }

            public DiagHandle(DiagHandle handle)
            {
                DiagID = handle.DiagID;
                TimeTakenMS = (DateTime.Now - handle.StartTime).TotalMilliseconds;
                StartTime = DateTime.MinValue;
            }
        }

        public List<DiagHandle> EndedHandles { get; set; } = new List<DiagHandle>();

        public SimplexDiagnostics() { }

        public DiagHandle BeginDiag(string id)
        {
            return new DiagHandle(id);
        }

        public void EndDiag(DiagHandle handle)
        {
            EndedHandles.Add(new DiagHandle(handle));
        }

        public void DebugLog(ISimplexLogger log)
        {
            log.Debug("-Request diag info");
            foreach (var d in EndedHandles)
                log.Debug($"  {d.DiagID} - {d.TimeTakenMS} ms");
        }
    }
}
