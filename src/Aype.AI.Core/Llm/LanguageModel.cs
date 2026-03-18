using System.Collections.Generic;
using System.Threading.Tasks;

namespace Aype.AI.Llm
{
    /// <summary>
    /// Represents a chat message (user, assistant, system, tool).
    /// Mirrors CoreMessage from @internal/ai-sdk-v4
    /// </summary>
    public class ChatMessage
    {
        public string Role { get; set; }
        public string Content { get; set; }
        public string Name { get; set; }
        public string ToolCallId { get; set; }
        public List<ToolCall> ToolCalls { get; set; }
    }

    /// <summary>
    /// Represents a single tool call from the model.
    /// </summary>
    public class ToolCall
    {
        public string Id { get; set; }
        public string ToolName { get; set; }
        public object Arguments { get; set; }
    }

    /// <summary>
    /// Result from a text generation call.
    /// Mirrors GenerateTextResult from packages/core/src/llm/model/base.types.ts
    /// </summary>
    public class GenerateTextResult
    {
        public string Text { get; set; }
        public string FinishReason { get; set; }
        public int PromptTokens { get; set; }
        public int CompletionTokens { get; set; }
        public List<ToolCall> ToolCalls { get; set; }
    }

    /// <summary>
    /// Configuration for model selection and parameters.
    /// Mirrors MastraModelConfig from packages/core/src/llm/model/shared.types.ts
    /// </summary>
    public class ModelConfig
    {
        /// <summary>Model identifier, e.g. "openai/gpt-4o" or "anthropic/claude-3-5-sonnet".</summary>
        public string ModelId { get; set; }

        /// <summary>Sampling temperature (0.0 – 2.0).</summary>
        public double? Temperature { get; set; }

        /// <summary>Maximum number of tokens to generate.</summary>
        public int? MaxTokens { get; set; }

        /// <summary>Top-p nucleus sampling value.</summary>
        public double? TopP { get; set; }

        /// <summary>Provider-specific extra parameters.</summary>
        public Dictionary<string, object> ProviderOptions { get; set; }
    }

    /// <summary>
    /// Options for a single generate/stream call.
    /// </summary>
    public class GenerateOptions
    {
        public List<ChatMessage> Messages { get; set; }
        public string SystemPrompt { get; set; }
        public int? MaxRetries { get; set; }
    }

    /// <summary>
    /// Abstraction over a language-model provider.
    /// Mirrors MastraLanguageModel from packages/core/src/llm/model/shared.types.ts
    /// </summary>
    public interface IMastraLanguageModel
    {
        string ModelId { get; }
        string Provider { get; }

        Task<GenerateTextResult> GenerateTextAsync(GenerateOptions options);
    }
}
