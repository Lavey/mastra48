using System.Collections.Generic;
using System.Threading.Tasks;
using Mastra48.Error;

namespace Mastra48.Logger
{
    /// <summary>
    /// No-operation logger that discards all log messages.
    /// Mirrors noopLogger from packages/core/src/logger/noop-logger.ts
    /// </summary>
    public class NoopLogger : IMastraLogger
    {
        public static readonly NoopLogger Instance = new NoopLogger();

        public void Debug(string message, params object[] args) { }
        public void Info(string message, params object[] args) { }
        public void Warn(string message, params object[] args) { }
        public void Error(string message, params object[] args) { }
        public void TrackException(MastraError error) { }

        public Dictionary<string, ILoggerTransport> GetTransports()
            => new Dictionary<string, ILoggerTransport>();

        public Task<LogListResult> ListLogs(string transportId, LogQueryParams queryParams = null)
            => Task.FromResult(new LogListResult
            {
                Logs = new List<BaseLogMessage>(),
                Total = 0,
                Page = queryParams?.Page ?? 1,
                PerPage = queryParams?.PerPage ?? 100,
                HasMore = false
            });

        public Task<LogListResult> ListLogsByRunId(string transportId, RunLogQueryParams queryParams)
            => Task.FromResult(new LogListResult
            {
                Logs = new List<BaseLogMessage>(),
                Total = 0,
                Page = queryParams?.Page ?? 1,
                PerPage = queryParams?.PerPage ?? 100,
                HasMore = false
            });
    }
}
