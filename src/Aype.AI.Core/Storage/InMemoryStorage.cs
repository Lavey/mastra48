using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Aype.AI.Storage
{
    /// <summary>
    /// In-memory implementation of IMastraStorage.
    /// Mirrors the in-memory storage patterns from packages/core/src/storage.
    /// </summary>
    public class InMemoryStorage : IMastraStorage
    {
        private readonly Dictionary<string, WorkflowRun> _workflowRuns
            = new Dictionary<string, WorkflowRun>();

        private readonly Dictionary<string, StorageThread> _threads
            = new Dictionary<string, StorageThread>();

        private readonly Dictionary<string, StorageMessage> _messages
            = new Dictionary<string, StorageMessage>();

        // ---- Workflow runs ----

        public Task<WorkflowRun> GetWorkflowRunByIdAsync(string runId)
        {
            _workflowRuns.TryGetValue(runId, out var run);
            return Task.FromResult(run);
        }

        public Task<List<WorkflowRun>> ListWorkflowRunsAsync(ListWorkflowRunsInput input = null)
        {
            var query = _workflowRuns.Values.AsEnumerable();

            if (input != null)
            {
                if (!string.IsNullOrEmpty(input.WorkflowId))
                    query = query.Where(r => r.WorkflowId == input.WorkflowId);

                if (input.Status.HasValue)
                    query = query.Where(r => r.Status == input.Status.Value);

                if (input.Offset.HasValue)
                    query = query.Skip(input.Offset.Value);

                if (input.Limit.HasValue)
                    query = query.Take(input.Limit.Value);
            }

            return Task.FromResult(query.ToList());
        }

        public Task<WorkflowRun> SaveWorkflowRunAsync(WorkflowRun run)
        {
            if (run == null) throw new ArgumentNullException("run");
            _workflowRuns[run.RunId] = run;
            return Task.FromResult(run);
        }

        public Task DeleteWorkflowRunAsync(string runId)
        {
            _workflowRuns.Remove(runId);
            return Task.FromResult(0);
        }

        // ---- Threads ----

        public Task<StorageThread> GetThreadByIdAsync(string threadId)
        {
            _threads.TryGetValue(threadId, out var thread);
            return Task.FromResult(thread);
        }

        public Task<List<StorageThread>> GetThreadsByResourceIdAsync(string resourceId)
        {
            var result = _threads.Values
                .Where(t => t.ResourceId == resourceId)
                .ToList();
            return Task.FromResult(result);
        }

        public Task<StorageThread> SaveThreadAsync(StorageThread thread)
        {
            if (thread == null) throw new ArgumentNullException("thread");
            _threads[thread.Id] = thread;
            return Task.FromResult(thread);
        }

        public Task DeleteThreadAsync(string threadId)
        {
            _threads.Remove(threadId);
            return Task.FromResult(0);
        }

        // ---- Messages ----

        public Task<List<StorageMessage>> GetMessagesByThreadIdAsync(string threadId, int? limit = null)
        {
            var query = _messages.Values
                .Where(m => m.ThreadId == threadId)
                .OrderBy(m => m.CreatedAt)
                .AsEnumerable();

            if (limit.HasValue)
                query = query.Take(limit.Value);

            return Task.FromResult(query.ToList());
        }

        public Task<StorageMessage> SaveMessageAsync(StorageMessage message)
        {
            if (message == null) throw new ArgumentNullException("message");
            _messages[message.Id] = message;
            return Task.FromResult(message);
        }

        public Task<List<StorageMessage>> SaveMessagesAsync(List<StorageMessage> messages)
        {
            if (messages == null) throw new ArgumentNullException("messages");
            foreach (var msg in messages)
                _messages[msg.Id] = msg;
            return Task.FromResult(messages);
        }

        public Task DeleteMessageAsync(string messageId)
        {
            _messages.Remove(messageId);
            return Task.FromResult(0);
        }
    }
}
