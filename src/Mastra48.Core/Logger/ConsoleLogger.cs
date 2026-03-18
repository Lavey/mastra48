using System;
using System.Collections.Generic;
using Mastra48.Error;

namespace Mastra48.Logger
{
    /// <summary>
    /// Console-based logger implementation.
    /// Mirrors ConsoleLogger from packages/core/src/logger/default-logger.ts
    /// </summary>
    public class ConsoleLogger : MastraLogger
    {
        public ConsoleLogger(string name = "Mastra", LogLevel level = LogLevel.INFO,
            Dictionary<string, ILoggerTransport> transports = null)
            : base(name, level, transports)
        {
        }

        public override void Debug(string message, params object[] args)
        {
            if (Level <= LogLevel.DEBUG)
                WriteLog("DEBUG", message, args);
        }

        public override void Info(string message, params object[] args)
        {
            if (Level <= LogLevel.INFO)
                WriteLog("INFO", message, args);
        }

        public override void Warn(string message, params object[] args)
        {
            if (Level <= LogLevel.WARN)
                WriteLog("WARN", message, args);
        }

        public override void Error(string message, params object[] args)
        {
            if (Level <= LogLevel.ERROR)
                WriteLog("ERROR", message, args);
        }

        public override void TrackException(MastraError error)
        {
            if (Level <= LogLevel.ERROR)
                Console.Error.WriteLine($"[{DateTime.UtcNow:O}] [{Name}] ERROR: {error}");
        }

        private void WriteLog(string level, string message, object[] args)
        {
            var formatted = args != null && args.Length > 0
                ? string.Format(message, args)
                : message;
            Console.WriteLine($"[{DateTime.UtcNow:O}] [{Name}] {level}: {formatted}");
        }
    }
}
