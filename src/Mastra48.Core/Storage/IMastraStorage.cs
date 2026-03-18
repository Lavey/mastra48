using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Mastra48.Storage
{
    /// <summary>
    /// Workflow run status values.
    /// Mirrors WorkflowRunStatus from packages/core/src/workflows/types.ts
    /// </summary>
    public enum WorkflowRunStatus
    {
        Pending,
        Running,
        Completed,
        Failed,
        Suspended
    }

    /// <summary>
    /// Represents a single workflow run record.
    /// </summary>
    public class WorkflowRun
    {
        public string RunId { get; set; }
        public string WorkflowId { get; set; }
        public WorkflowRunStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public Dictionary<string, object> Input { get; set; }
        public Dictionary<string, object> Output { get; set; }
        public string ErrorMessage { get; set; }
    }

    /// <summary>
    /// Thread/conversation message.
    /// </summary>
    public class StorageMessage
    {
        public string Id { get; set; }
        public string ThreadId { get; set; }
        public string ResourceId { get; set; }
        public string Role { get; set; }
        public string Content { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>
    /// Conversation thread metadata.
    /// </summary>
    public class StorageThread
    {
        public string Id { get; set; }
        public string ResourceId { get; set; }
        public string Title { get; set; }
        public Dictionary<string, object> Metadata { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    /// <summary>
    /// Parameters for listing workflow runs.
    /// </summary>
    public class ListWorkflowRunsInput
    {
        public string WorkflowId { get; set; }
        public int? Limit { get; set; }
        public int? Offset { get; set; }
        public WorkflowRunStatus? Status { get; set; }
    }

    /// <summary>
    /// Composite storage interface combining workflow and memory storage.
    /// Mirrors MastraCompositeStore from packages/core/src/storage/index.ts
    /// </summary>
    public interface IMastraStorage
    {
        // ---- Workflow runs ----
        Task<WorkflowRun> GetWorkflowRunByIdAsync(string runId);
        Task<List<WorkflowRun>> ListWorkflowRunsAsync(ListWorkflowRunsInput input = null);
        Task<WorkflowRun> SaveWorkflowRunAsync(WorkflowRun run);
        Task DeleteWorkflowRunAsync(string runId);

        // ---- Threads ----
        Task<StorageThread> GetThreadByIdAsync(string threadId);
        Task<List<StorageThread>> GetThreadsByResourceIdAsync(string resourceId);
        Task<StorageThread> SaveThreadAsync(StorageThread thread);
        Task DeleteThreadAsync(string threadId);

        // ---- Messages ----
        Task<List<StorageMessage>> GetMessagesByThreadIdAsync(string threadId, int? limit = null);
        Task<StorageMessage> SaveMessageAsync(StorageMessage message);
        Task<List<StorageMessage>> SaveMessagesAsync(List<StorageMessage> messages);
        Task DeleteMessageAsync(string messageId);
    }
}
