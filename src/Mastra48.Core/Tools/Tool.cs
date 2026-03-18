using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Mastra48.Tools
{
    /// <summary>
    /// A type-safe tool that agents and workflows can call to perform specific actions.
    /// Mirrors the Tool class from packages/core/src/tools/tool.ts
    ///
    /// <example>
    /// Basic usage:
    /// <code>
    ///   var weatherTool = new Tool&lt;WeatherInput, WeatherOutput&gt;(
    ///       id: "get-weather",
    ///       description: "Get weather for a location",
    ///       execute: async (input, ctx) => await FetchWeather(input.Location));
    /// </code>
    /// </example>
    /// </summary>
    /// <typeparam name="TInput">Input parameter type.</typeparam>
    /// <typeparam name="TOutput">Output return type.</typeparam>
    public class Tool<TInput, TOutput> : IToolAction<TInput, TOutput>
    {
        private readonly Func<TInput, ToolExecutionContext, Task<TOutput>> _executeFunc;

        /// <inheritdoc/>
        public string Id { get; }

        /// <inheritdoc/>
        public string Description { get; }

        /// <inheritdoc/>
        public bool RequireApproval { get; }

        /// <summary>Optional provider-specific options.</summary>
        public Dictionary<string, Dictionary<string, object>> ProviderOptions { get; }

        /// <summary>Examples of valid tool inputs.</summary>
        public List<Dictionary<string, object>> InputExamples { get; }

        /// <summary>
        /// Creates a new Tool instance.
        /// </summary>
        /// <param name="id">Unique identifier for the tool.</param>
        /// <param name="description">Description of what the tool does.</param>
        /// <param name="execute">Async function that performs the tool's operations.</param>
        /// <param name="requireApproval">Whether explicit user approval is required before execution.</param>
        /// <param name="providerOptions">Provider-specific options.</param>
        /// <param name="inputExamples">Examples of valid tool inputs.</param>
        public Tool(
            string id,
            string description,
            Func<TInput, ToolExecutionContext, Task<TOutput>> execute,
            bool requireApproval = false,
            Dictionary<string, Dictionary<string, object>> providerOptions = null,
            List<Dictionary<string, object>> inputExamples = null)
        {
            if (string.IsNullOrEmpty(id))
                throw new ArgumentException("Tool id cannot be null or empty.", "id");
            if (execute == null)
                throw new ArgumentNullException("execute");

            Id = id;
            Description = description ?? string.Empty;
            RequireApproval = requireApproval;
            ProviderOptions = providerOptions;
            InputExamples = inputExamples;
            _executeFunc = execute;
        }

        /// <inheritdoc/>
        public Task<TOutput> ExecuteAsync(TInput input, ToolExecutionContext context = null)
        {
            return _executeFunc(input, context);
        }
    }

    /// <summary>
    /// Factory helper that mirrors createTool() from packages/core/src/tools/tool.ts
    /// </summary>
    public static class ToolFactory
    {
        /// <summary>
        /// Creates a typed tool from configuration parameters.
        /// </summary>
        public static Tool<TInput, TOutput> CreateTool<TInput, TOutput>(
            string id,
            string description,
            Func<TInput, ToolExecutionContext, Task<TOutput>> execute,
            bool requireApproval = false,
            Dictionary<string, Dictionary<string, object>> providerOptions = null)
        {
            return new Tool<TInput, TOutput>(id, description, execute, requireApproval, providerOptions);
        }
    }
}
