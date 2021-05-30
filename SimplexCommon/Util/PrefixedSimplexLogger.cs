using System;
using System.Collections.Generic;
using System.Text;

namespace Simplex.Util
{
    public class PrefixedSimplexLogger : SimplexLoggerBase
    {
        private ISimplexLogger baseLogger;
        private string prefix;

        public PrefixedSimplexLogger(ISimplexLogger baseLog, string pref)
        {
            baseLogger = baseLog;
            prefix = pref;
        }

        public override void Debug(object msg)
        {
            baseLogger.Debug($"[{prefix}] {msg}");
        }

        public override void Error(object msg)
        {
            baseLogger.Error($"[{prefix}] {msg}");
        }

        public override void Info(object msg)
        {
            baseLogger.Info($"[{prefix}] {msg}");
        }

        public override void Log(object msg, SimplexLogType logType)
        {
            baseLogger.Log($"[{prefix}] {msg}", logType);
        }

        public override void Warn(object msg)
        {
            baseLogger.Warn($"[{prefix}] {msg}");
        }
    }
}
