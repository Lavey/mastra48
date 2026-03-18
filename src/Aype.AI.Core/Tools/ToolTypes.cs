using System.Collections.Generic;
using System.Threading.Tasks;

namespace Aype.AI.Tools
{
    /// <summary>
    /// Execution context provided to a tool at runtime.
    /// Mirrors ToolExecutionContext from packages/core/src/tools/types.ts
    /// </summary>
    public class ToolExecutionContext
    {
        /// <summary>ID of the tool call (set by the LLM layer).</summary>
        public string ToolCallId { get; set; }

        /// <summary>Reference to the Mastra instance (may be null outside agent execution).</summary>
        public object Mastra { get; set; }

        /// <summary>Resource identifier for memory scoping.</summary>
        public string ResourceId { get; set; }

        /// <summary>Thread identifier for memory scoping.</summary>
        public string ThreadId { get; set; }

        /// <summary>Arbitrary additional metadata.</summary>
        public Dictionary<string, object> Metadata { get; set; }
    }

    /// <summary>
    /// Result returned when tool input/output validation fails.
    /// Mirrors ValidationError from packages/core/src/tools/validation.ts
    /// </summary>
    public class ValidationError
    {
        public string ToolId { get; set; }
        public bool IsValidationError { get; } = true;
        public List<string> Errors { get; set; }

        public ValidationError(string toolId, List<string> errors)
        {
            ToolId = toolId;
            Errors = errors;
        }
    }

    /// <summary>
    /// Defines the contract for a tool that agents can invoke.
    /// Mirrors ToolAction from packages/core/src/tools/types.ts
    /// </summary>
    public interface IToolAction
    {
        string Id { get; }
        string Description { get; }
        bool RequireApproval { get; }
    }

    /// <summary>
    /// Strongly-typed tool action.
    /// </summary>
    /// <typeparam name="TInput">Input parameter type.</typeparam>
    /// <typeparam name="TOutput">Output return type.</typeparam>
    public interface IToolAction<TInput, TOutput> : IToolAction
    {
        Task<TOutput> ExecuteAsync(TInput input, ToolExecutionContext context = null);
    }
}
