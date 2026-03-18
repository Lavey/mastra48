using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Mastra48.Error;
using Mastra48.Llm;
using Mastra48.Logger;
using Mastra48.Memory;
using Mastra48.Storage;
using Mastra48.Tools;

namespace Mastra48.Agent
{
    /// <summary>
    /// The Agent class is the foundation for creating AI agents in Mastra.
    /// It provides methods for generating responses, streaming interactions,
    /// managing memory, and handling tools.
    ///
    /// Mirrors the Agent class from packages/core/src/agent/agent.ts
    ///
    /// <example>
    /// <code>
    ///   var agent = new Agent(new AgentConfig
    ///   {
    ///       Id = "my-agent",
    ///       Name = "My Agent",
    ///       Instructions = "You are a helpful assistant",
    ///       Model = new ModelConfig { ModelId = "openai/gpt-4o" },
    ///       Tools = new Dictionary&lt;string, IToolAction&gt; { { "calculator", calcTool } }
    ///   });
    ///
    ///   var result = await agent.GenerateAsync(new AgentGenerateOptions { Prompt = "Hello!" });
    ///   Console.WriteLine(result.Text);
    /// </code>
    /// </example>
    /// </summary>
    public class Agent
    {
        private readonly AgentConfig _config;
        private IMastraLogger _logger;
        private IMastraLanguageModel _languageModel;

        /// <summary>Unique identifier for this agent.</summary>
        public string Id => _config.Id ?? _config.Name;

        /// <summary>Human-readable name.</summary>
        public string Name => _config.Name;

        /// <summary>System instructions.</summary>
        public AgentInstructions Instructions => _config.Instructions;

        /// <summary>Model configuration.</summary>
        public ModelConfig Model => _config.Model;

        /// <summary>Tools registered with this agent.</summary>
        public IReadOnlyDictionary<string, IToolAction> Tools
            => _config.Tools ?? new Dictionary<string, IToolAction>();

        /// <summary>Memory implementation used by this agent.</summary>
        public MastraMemory Memory => _config.Memory;

        /// <summary>
        /// Creates a new Agent from configuration.
        /// </summary>
        public Agent(AgentConfig config)
        {
            if (config == null) throw new ArgumentNullException("config");
            if (string.IsNullOrEmpty(config.Name))
                throw new ArgumentException("Agent Name cannot be null or empty.", "config");

            _config = config;
            _logger = NoopLogger.Instance;
        }

        /// <summary>
        /// Injects the logger (called by the Mastra orchestrator).
        /// </summary>
        public void SetLogger(IMastraLogger logger)
        {
            _logger = logger ?? NoopLogger.Instance;
        }

        /// <summary>
        /// Injects a language-model instance (called by the Mastra orchestrator or tests).
        /// </summary>
        public void SetLanguageModel(IMastraLanguageModel model)
        {
            _languageModel = model;
        }

        /// <summary>
        /// Generates a text response.
        /// Mirrors Agent.generate() from packages/core/src/agent/agent.ts
        /// </summary>
        public async Task<AgentGenerateResult> GenerateAsync(AgentGenerateOptions options)
        {
            if (options == null) throw new ArgumentNullException("options");
            if (_languageModel == null)
                throw new MastraError(new ErrorDefinition
                {
                    Id = "AGENT_NO_MODEL",
                    Domain = ErrorDomain.AGENT,
                    Category = ErrorCategory.USER,
                    Text = $"Agent '{Id}' has no language model configured. " +
                           "Call SetLanguageModel() or configure via Mastra."
                });

            _logger.Debug("Agent '{0}' generating response", Id);

            var messages = await BuildMessagesAsync(options);

            var result = await _languageModel.GenerateTextAsync(new GenerateOptions
            {
                Messages = messages,
                SystemPrompt = _config.Instructions?.Resolve(),
                MaxRetries = options.MaxRetries ?? _config.MaxRetries
            });

            // Persist to memory if configured
            if (_config.Memory != null && !string.IsNullOrEmpty(options.ThreadId))
            {
                var userMsg = new StorageMessage
                {
                    Id = Guid.NewGuid().ToString(),
                    ThreadId = options.ThreadId,
                    ResourceId = options.ResourceId,
                    Role = "user",
                    Content = options.Prompt ?? string.Empty,
                    CreatedAt = DateTime.UtcNow
                };

                var assistantMsg = new StorageMessage
                {
                    Id = Guid.NewGuid().ToString(),
                    ThreadId = options.ThreadId,
                    ResourceId = options.ResourceId,
                    Role = "assistant",
                    Content = result.Text ?? string.Empty,
                    CreatedAt = DateTime.UtcNow
                };

                await _config.Memory.SaveMessagesAsync(
                    options.ThreadId,
                    new List<StorageMessage> { userMsg, assistantMsg });
            }

            _logger.Debug("Agent '{0}' generation complete. FinishReason={1}", Id, result.FinishReason);

            return new AgentGenerateResult
            {
                Text = result.Text,
                FinishReason = result.FinishReason,
                PromptTokens = result.PromptTokens,
                CompletionTokens = result.CompletionTokens,
                ToolCalls = result.ToolCalls
            };
        }

        private async Task<List<ChatMessage>> BuildMessagesAsync(AgentGenerateOptions options)
        {
            var messages = new List<ChatMessage>();

            // Include memory context if available
            if (_config.Memory != null && !string.IsNullOrEmpty(options.ThreadId))
            {
                var history = await _config.Memory.GetMessagesAsync(options.ThreadId);
                foreach (var msg in history)
                {
                    messages.Add(new ChatMessage { Role = msg.Role, Content = msg.Content });
                }
            }

            // Append any explicitly passed messages
            if (options.Messages != null)
                messages.AddRange(options.Messages);

            // Append the new user prompt
            if (!string.IsNullOrEmpty(options.Prompt))
                messages.Add(new ChatMessage { Role = "user", Content = options.Prompt });

            return messages;
        }
    }
}
