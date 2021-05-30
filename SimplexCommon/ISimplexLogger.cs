using System;
using System.Collections.Generic;
using System.Text;

namespace Simplex
{
    [Flags]
    public enum SimplexLogType
    {
        Debug = 0x0001,
        Info = 0x0002,
        Warn = 0x0004,
        Error = 0x0008
    }

    public interface ISimplexLogger
    {
        void Log  (object msg, SimplexLogType logType);
        void Debug(object msg);
        void Info (object msg);
        void Warn (object msg);
        void Error(object msg);
        void SetLogFlag(SimplexLogType type);
        void UnsetLogFlag(SimplexLogType type);
        bool IsLogFlagSet(SimplexLogType type);
    }

    public abstract class SimplexLoggerBase : ISimplexLogger
    {
        private SimplexLogType flags;

        public abstract void Log  (object msg, SimplexLogType logType);
        public abstract void Debug(object msg);
        public abstract void Info (object msg);
        public abstract void Warn (object msg);
        public abstract void Error(object msg);

        public SimplexLoggerBase()
        {
            flags = SimplexLogType.Debug | SimplexLogType.Info | SimplexLogType.Warn | SimplexLogType.Error;
        }

        public void SetLogFlag(SimplexLogType type)
        {
            flags |= type;
        }

        public void UnsetLogFlag(SimplexLogType type)
        {
            flags &= ~type;
        }

        public bool IsLogFlagSet(SimplexLogType type)
        {
            return (flags & type) > 0;
        }

        public SimplexLogType GetFlags()
        {
            return flags;
        }
    }

    public class ConsoleLogger : SimplexLoggerBase
    {
        public override void Log(object msg, SimplexLogType logType)
        {
            if (!IsLogFlagSet(logType))
                return;

            ConsoleColor f = ConsoleColor.White;
            ConsoleColor b = ConsoleColor.Black;
            switch (logType)
            {
                case SimplexLogType.Debug:
                    f = ConsoleColor.White;
                    b = ConsoleColor.DarkBlue;
                    break;
                case SimplexLogType.Info:
                    f = ConsoleColor.White;
                    b = ConsoleColor.Black;
                    break;
                case SimplexLogType.Warn:
                    f = ConsoleColor.Yellow;
                    b = ConsoleColor.Black;
                    break;
                case SimplexLogType.Error:
                    f = ConsoleColor.White;
                    b = ConsoleColor.Red;
                    break;
            }

            Console.ForegroundColor = f;
            Console.BackgroundColor = b;

            Console.WriteLine(msg);
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

        public override void Warn(object msg)
        {
            Log(msg, SimplexLogType.Warn);
        }
    }
}
