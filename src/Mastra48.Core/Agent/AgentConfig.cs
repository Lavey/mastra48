using System;
using System.Collections.Generic;
using Mastra48.Llm;
using Mastra48.Memory;
using Mastra48.Tools;

namespace Mastra48.Agent
{
    /// <summary>
    /// Instructions for an agent, can be a static string or dynamic (resolved at runtime).
    /// Mirrors AgentInstructions from packages/core/src/agent/types.ts
    /// </summary>
    public class AgentInstructions
    {
        private readonly string _static;
        private readonly Func<string> _dynamic;

        public AgentInstructions(string staticText)
        {
            _static = staticText;
        }

        public AgentInstructions(Func<string> dynamicResolver)
        {
            _dynamic = dynamicResolver ?? throw new ArgumentNullException("dynamicResolver");
        }

        public string Resolve()
        {
            return _dynamic != null ? _dynamic() : _static;
        }

        public static implicit operator AgentInstructions(string s) => new AgentInstructions(s);
    }

    /// <summary>
    /// Configuration for creating an Agent instance.
    /// Mirrors AgentConfig from packages/core/src/agent/types.ts
    /// </summary>
    public class AgentConfig
    {
        /// <summary>
        /// Unique identifier for the agent.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Human-readable display name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// System prompt / instructions for the agent.
        /// </summary>
        public AgentInstructions Instructions { get; set; }

        /// <summary>
        /// Model configuration or model ID string (e.g. "openai/gpt-4o").
        /// </summary>
        public ModelConfig Model { get; set; }

        /// <summary>
        /// Optional registered tools available to this agent.
        /// </summary>
        public Dictionary<string, IToolAction> Tools { get; set; }

        /// <summary>
        /// Optional memory implementation for conversation history.
        /// </summary>
        public MastraMemory Memory { get; set; }

        /// <summary>
        /// Maximum number of retries on model errors.
        /// </summary>
        public int MaxRetries { get; set; } = 0;
    }

    /// <summary>
    /// Options for agent generate/stream calls.
    /// Mirrors AgentGenerateOptions from packages/core/src/agent/types.ts
    /// </summary>
    public class AgentGenerateOptions
    {
        /// <summary>User prompt text or list of messages.</summary>
        public string Prompt { get; set; }

        /// <summary>Thread ID for conversation memory context.</summary>
        public string ThreadId { get; set; }

        /// <summary>Resource ID for memory scoping.</summary>
        public string ResourceId { get; set; }

        /// <summary>Override default max retries for this call.</summary>
        public int? MaxRetries { get; set; }

        /// <summary>Additional messages to include in context.</summary>
        public List<Mastra48.Llm.ChatMessage> Messages { get; set; }
    }

    /// <summary>
    /// Result from agent text generation.
    /// </summary>
    public class AgentGenerateResult
    {
        public string Text { get; set; }
        public string FinishReason { get; set; }
        public int PromptTokens { get; set; }
        public int CompletionTokens { get; set; }
        public List<Llm.ToolCall> ToolCalls { get; set; }
    }
}
