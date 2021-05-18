using Simplex;
using System;
using System.Collections.Generic;
using System.Text;

namespace SimplexLambda
{
    public class LambdaLogger : ConsoleLogger
    {
        public List<object> logs = new List<object>();

        public override void Log(object msg, SimplexLogType logType)
        {
            if (!IsLogFlagSet(logType))
                return;

            base.Log(msg, logType);

            logs.Add(msg);
        }
    }
}
