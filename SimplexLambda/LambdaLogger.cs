using Simplex;
using System;
using System.Collections.Generic;
using System.Text;

namespace SimplexLambda
{
    public class LambdaLogger : SimplexLoggerBase
    {
        public List<object> logs = new List<object>();

        ISimplexLogger baseLogger;

        public LambdaLogger(ISimplexLogger logger)
        {
            baseLogger = logger;
        }

        public override void Debug(object msg)
        {
            Log(msg, SimplexLogType.Debug);
        }

        public override void Error(object msg)
        {
            Log(msg, SimplexLogType.Error);
        }

        public override void Info(object msg)
        {
            Log(msg, SimplexLogType.Info);
        }

        public override void Log(object msg, SimplexLogType logType)
        {
            if (!IsLogFlagSet(logType))
                return;

            baseLogger.Log(msg, logType);

            logs.Add(msg);
        }

        public override void Warn(object msg)
        {
            Log(msg, SimplexLogType.Warn);
        }
    }
}
