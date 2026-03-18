using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Aype.AI.Storage;

namespace Aype.AI.Memory
{
    /// <summary>
    /// Configuration for memory retrieval.
    /// Mirrors MemoryConfig from packages/core/src/memory/types.ts
    /// </summary>
    public class MemoryConfig
    {
        /// <summary>Maximum number of recent messages to retrieve.</summary>
        public int? LastMessages { get; set; }

        /// <summary>Whether to enable semantic search over past messages.</summary>
        public bool SemanticRecall { get; set; }

        /// <summary>Working memory window size in tokens.</summary>
        public int? WorkingMemoryWindowSize { get; set; }
    }

    /// <summary>
    /// Abstract base class for memory implementations.
    /// Mirrors MastraMemory from packages/core/src/memory/memory.ts
    /// </summary>
    public abstract class MastraMemory
    {
        protected IMastraStorage Storage { get; }
        protected MemoryConfig Config { get; }

        protected MastraMemory(IMastraStorage storage = null, MemoryConfig config = null)
        {
            Storage = storage ?? new InMemoryStorage();
            Config = config ?? new MemoryConfig();
        }

        /// <summary>
        /// Retrieves messages for a thread (conversation), applying memory configuration.
        /// </summary>
        public abstract Task<List<StorageMessage>> GetMessagesAsync(
            string threadId,
            string resourceId = null,
            MemoryConfig config = null);

        /// <summary>
        /// Saves one or more messages to the thread.
        /// </summary>
        public abstract Task<List<StorageMessage>> SaveMessagesAsync(
            string threadId,
            List<StorageMessage> messages);

        /// <summary>
        /// Gets or creates a thread for the given resource and thread IDs.
        /// </summary>
        public abstract Task<StorageThread> GetOrCreateThreadAsync(
            string resourceId,
            string threadId = null,
            string title = null,
            Dictionary<string, object> metadata = null);

        /// <summary>
        /// Deletes a thread and all its messages.
        /// </summary>
        public abstract Task DeleteThreadAsync(string threadId);
    }

    /// <summary>
    /// Simple in-memory implementation of MastraMemory backed by InMemoryStorage.
    /// Mirrors the in-memory memory from packages/core/src/memory/mock.ts
    /// </summary>
    public class InMemoryMemory : MastraMemory
    {
        public InMemoryMemory(MemoryConfig config = null)
            : base(new InMemoryStorage(), config)
        {
        }

        public override async Task<List<StorageMessage>> GetMessagesAsync(
            string threadId,
            string resourceId = null,
            MemoryConfig config = null)
        {
            var effectiveConfig = config ?? Config;
            var messages = await Storage.GetMessagesByThreadIdAsync(threadId);

            if (effectiveConfig.LastMessages.HasValue && messages.Count > effectiveConfig.LastMessages.Value)
                messages = messages.GetRange(
                    messages.Count - effectiveConfig.LastMessages.Value,
                    effectiveConfig.LastMessages.Value);

            return messages;
        }

        public override Task<List<StorageMessage>> SaveMessagesAsync(
            string threadId,
            List<StorageMessage> messages)
        {
            return Storage.SaveMessagesAsync(messages);
        }

        public override async Task<StorageThread> GetOrCreateThreadAsync(
            string resourceId,
            string threadId = null,
            string title = null,
            Dictionary<string, object> metadata = null)
        {
            if (!string.IsNullOrEmpty(threadId))
            {
                var existing = await Storage.GetThreadByIdAsync(threadId);
                if (existing != null) return existing;
            }

            var thread = new StorageThread
            {
                Id = threadId ?? Guid.NewGuid().ToString(),
                ResourceId = resourceId,
                Title = title,
                Metadata = metadata ?? new Dictionary<string, object>(),
                CreatedAt = DateTime.UtcNow
            };

            return await Storage.SaveThreadAsync(thread);
        }

        public override Task DeleteThreadAsync(string threadId)
        {
            return Storage.DeleteThreadAsync(threadId);
        }
    }
}
