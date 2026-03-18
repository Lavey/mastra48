using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Aype.AI.Workflows
{
    /// <summary>
    /// Result of a single step execution.
    /// Mirrors StepResult from packages/core/src/workflows/types.ts
    /// </summary>
    public class StepResult
    {
        /// <summary>The step output value.</summary>
        public object Output { get; set; }

        /// <summary>Step execution status.</summary>
        public string Status { get; set; }

        /// <summary>Error message if step failed.</summary>
        public string ErrorMessage { get; set; }

        public static StepResult Success(object output)
            => new StepResult { Output = output, Status = "success" };

        public static StepResult Failed(string error)
            => new StepResult { Output = null, Status = "failed", ErrorMessage = error };

        public static StepResult Suspended(object output = null)
            => new StepResult { Output = output, Status = "suspended" };
    }

    /// <summary>
    /// Execution context provided to each step.
    /// Mirrors the context parameter in step execute functions from packages/core/src/workflows.
    /// </summary>
    public class StepExecutionContext
    {
        /// <summary>Input data for this step.</summary>
        public object Input { get; set; }

        /// <summary>Results of previously executed steps (keyed by step ID).</summary>
        public Dictionary<string, StepResult> PreviousStepResults { get; set; }

        /// <summary>The workflow's run ID.</summary>
        public string RunId { get; set; }

        /// <summary>Callback to request human-in-the-loop approval (suspend).</summary>
        public Func<object, Task> Suspend { get; set; }
    }

    /// <summary>
    /// Step configuration / execution function container.
    /// Mirrors Step from packages/core/src/workflows/step.ts
    /// </summary>
    public class Step
    {
        /// <summary>Unique step identifier.</summary>
        public string Id { get; }

        /// <summary>Human-readable description of what this step does.</summary>
        public string Description { get; }

        private readonly Func<StepExecutionContext, Task<object>> _execute;

        /// <summary>
        /// Creates a new step.
        /// </summary>
        public Step(
            string id,
            Func<StepExecutionContext, Task<object>> execute,
            string description = null)
        {
            if (string.IsNullOrEmpty(id))
                throw new ArgumentException("Step id cannot be null or empty.", "id");
            if (execute == null)
                throw new ArgumentNullException("execute");

            Id = id;
            Description = description ?? string.Empty;
            _execute = execute;
        }

        /// <summary>Executes this step and returns its result.</summary>
        public async Task<StepResult> RunAsync(StepExecutionContext context)
        {
            try
            {
                var output = await _execute(context);
                return StepResult.Success(output);
            }
            catch (Exception ex)
            {
                return StepResult.Failed(ex.Message);
            }
        }
    }

    /// <summary>
    /// Factory helper mirroring createStep() from packages/core/src/workflows/workflow.ts
    /// </summary>
    public static class StepFactory
    {
        public static Step CreateStep(
            string id,
            Func<StepExecutionContext, Task<object>> execute,
            string description = null)
        {
            return new Step(id, execute, description);
        }
    }
}
