using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Aype.AI.Error;

namespace Aype.AI.Logger
{
    /// <summary>
    /// Base log message structure returned from transports.
    /// </summary>
    public class BaseLogMessage
    {
        public string Message { get; set; }
        public LogLevel Level { get; set; }
        public DateTime Timestamp { get; set; }
        public string RunId { get; set; }
        public Dictionary<string, object> Fields { get; set; }
    }

    /// <summary>
    /// Paged result of log messages.
    /// </summary>
    public class LogListResult
    {
        public List<BaseLogMessage> Logs { get; set; }
        public int Total { get; set; }
        public int Page { get; set; }
        public int PerPage { get; set; }
        public bool HasMore { get; set; }
    }

    /// <summary>
    /// Logger transport interface for pluggable log destinations.
    /// Mirrors LoggerTransport from packages/core/src/logger/transport.ts
    /// </summary>
    public interface ILoggerTransport
    {
        Task<LogListResult> ListLogs(LogQueryParams queryParams = null);
        Task<LogListResult> ListLogsByRunId(RunLogQueryParams queryParams);
    }

    /// <summary>Query parameters for listing logs.</summary>
    public class LogQueryParams
    {
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public LogLevel? LogLevel { get; set; }
        public Dictionary<string, object> Filters { get; set; }
        public int Page { get; set; } = 1;
        public int PerPage { get; set; } = 100;
    }

    /// <summary>Query parameters for listing logs by run ID.</summary>
    public class RunLogQueryParams : LogQueryParams
    {
        public string RunId { get; set; }
    }

    /// <summary>
    /// Logger interface.
    /// Mirrors IMastraLogger from packages/core/src/logger/logger.ts
    /// </summary>
    public interface IMastraLogger
    {
        void Debug(string message, params object[] args);
        void Info(string message, params object[] args);
        void Warn(string message, params object[] args);
        void Error(string message, params object[] args);
        void TrackException(MastraError error);

        Dictionary<string, ILoggerTransport> GetTransports();

        Task<LogListResult> ListLogs(
            string transportId,
            LogQueryParams queryParams = null);

        Task<LogListResult> ListLogsByRunId(
            string transportId,
            RunLogQueryParams queryParams);
    }

    /// <summary>
    /// Abstract base implementation of IMastraLogger.
    /// Mirrors MastraLogger from packages/core/src/logger/logger.ts
    /// </summary>
    public abstract class MastraLogger : IMastraLogger
    {
        protected string Name { get; }
        protected LogLevel Level { get; }
        protected Dictionary<string, ILoggerTransport> Transports { get; }

        protected MastraLogger(string name = "Mastra", LogLevel level = LogLevel.ERROR,
            Dictionary<string, ILoggerTransport> transports = null)
        {
            Name = name;
            Level = level;
            Transports = transports ?? new Dictionary<string, ILoggerTransport>();
        }

        public abstract void Debug(string message, params object[] args);
        public abstract void Info(string message, params object[] args);
        public abstract void Warn(string message, params object[] args);
        public abstract void Error(string message, params object[] args);

        public virtual void TrackException(MastraError error) { }

        public Dictionary<string, ILoggerTransport> GetTransports() => Transports;

        public virtual async Task<LogListResult> ListLogs(string transportId, LogQueryParams queryParams = null)
        {
            var empty = BuildEmptyResult(queryParams);
            if (string.IsNullOrEmpty(transportId) || !Transports.ContainsKey(transportId))
                return empty;

            return await Transports[transportId].ListLogs(queryParams) ?? empty;
        }

        public virtual async Task<LogListResult> ListLogsByRunId(string transportId, RunLogQueryParams queryParams)
        {
            var empty = BuildEmptyResult(queryParams);
            if (string.IsNullOrEmpty(transportId) || !Transports.ContainsKey(transportId))
                return empty;
            if (queryParams == null || string.IsNullOrEmpty(queryParams.RunId))
                return empty;

            return await Transports[transportId].ListLogsByRunId(queryParams) ?? empty;
        }

        private static LogListResult BuildEmptyResult(LogQueryParams p)
        {
            return new LogListResult
            {
                Logs = new List<BaseLogMessage>(),
                Total = 0,
                Page = p?.Page ?? 1,
                PerPage = p?.PerPage ?? 100,
                HasMore = false
            };
        }
    }
}
