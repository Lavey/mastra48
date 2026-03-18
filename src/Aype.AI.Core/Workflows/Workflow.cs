using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Aype.AI.Logger;
using Aype.AI.Storage;

namespace Aype.AI.Workflows
{
    /// <summary>
    /// Result of a complete workflow run.
    /// Mirrors WorkflowResult from packages/core/src/workflows/types.ts
    /// </summary>
    public class WorkflowResult
    {
        public string RunId { get; set; }
        public WorkflowRunStatus Status { get; set; }
        public Dictionary<string, StepResult> StepResults { get; set; }
        public object FinalOutput { get; set; }
        public string ErrorMessage { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
    }

    /// <summary>
    /// Options for starting a workflow run.
    /// Mirrors WorkflowRunStartOptions from packages/core/src/workflows/types.ts
    /// </summary>
    public class WorkflowRunOptions
    {
        /// <summary>Input data for the workflow's first step.</summary>
        public object Input { get; set; }

        /// <summary>Optional run ID override (auto-generated if not provided).</summary>
        public string RunId { get; set; }

        /// <summary>Resource ID for memory/storage scoping.</summary>
        public string ResourceId { get; set; }

        /// <summary>Thread ID for memory/storage scoping.</summary>
        public string ThreadId { get; set; }
    }

    /// <summary>
    /// The Workflow class orchestrates a sequence of steps with built-in error handling,
    /// suspend/resume, and storage persistence.
    ///
    /// Mirrors the Workflow class from packages/core/src/workflows/workflow.ts
    ///
    /// <example>
    /// <code>
    ///   var workflow = new Workflow("process-order")
    ///       .Then(validateStep)
    ///       .Then(paymentStep)
    ///       .Then(fulfillmentStep);
    ///
    ///   var result = await workflow.RunAsync(new WorkflowRunOptions { Input = orderData });
    /// </code>
    /// </example>
    /// </summary>
    public class Workflow
    {
        private readonly List<Step> _steps = new List<Step>();
        private IMastraLogger _logger;
        private IMastraStorage _storage;

        /// <summary>Unique workflow identifier.</summary>
        public string Id { get; }

        /// <summary>Optional human-readable description.</summary>
        public string Description { get; }

        /// <summary>Read-only view of registered steps.</summary>
        public IReadOnlyList<Step> Steps => _steps;

        public Workflow(string id, string description = null)
        {
            if (string.IsNullOrEmpty(id))
                throw new ArgumentException("Workflow id cannot be null or empty.", "id");

            Id = id;
            Description = description ?? string.Empty;
            _logger = NoopLogger.Instance;
        }

        /// <summary>
        /// Appends a step to the workflow execution chain.
        /// Mirrors .then() from the TypeScript Workflow DSL.
        /// </summary>
        public Workflow Then(Step step)
        {
            if (step == null) throw new ArgumentNullException("step");
            _steps.Add(step);
            return this;
        }

        /// <summary>
        /// Injects the logger (called by the Mastra orchestrator).
        /// </summary>
        public void SetLogger(IMastraLogger logger)
        {
            _logger = logger ?? NoopLogger.Instance;
        }

        /// <summary>
        /// Injects a storage implementation for persisting run state.
        /// </summary>
        public void SetStorage(IMastraStorage storage)
        {
            _storage = storage;
        }

        /// <summary>
        /// Executes the workflow from start to finish.
        /// Mirrors Workflow.execute() / Workflow.start() from packages/core/src/workflows/workflow.ts
        /// </summary>
        public async Task<WorkflowResult> RunAsync(WorkflowRunOptions options = null)
        {
            var runId = options?.RunId ?? Guid.NewGuid().ToString();
            var input = options?.Input;
            var startedAt = DateTime.UtcNow;

            _logger.Info("Workflow '{0}' starting (runId={1})", Id, runId);

            var stepResults = new Dictionary<string, StepResult>();
            WorkflowResult result;

            // Persist initial run state
            if (_storage != null)
            {
                await _storage.SaveWorkflowRunAsync(new WorkflowRun
                {
                    RunId = runId,
                    WorkflowId = Id,
                    Status = WorkflowRunStatus.Running,
                    CreatedAt = startedAt,
                    Input = input != null
                        ? new Dictionary<string, object> { { "data", input } }
                        : null
                });
            }

            try
            {
                object currentInput = input;

                foreach (var step in _steps)
                {
                    _logger.Debug("Workflow '{0}' executing step '{1}'", Id, step.Id);

                    var ctx = new StepExecutionContext
                    {
                        Input = currentInput,
                        PreviousStepResults = new Dictionary<string, StepResult>(stepResults),
                        RunId = runId
                    };

                    var stepResult = await step.RunAsync(ctx);
                    stepResults[step.Id] = stepResult;

                    if (stepResult.Status == "failed")
                    {
                        _logger.Warn("Workflow '{0}' step '{1}' failed: {2}",
                            Id, step.Id, stepResult.ErrorMessage);

                        result = new WorkflowResult
                        {
                            RunId = runId,
                            Status = WorkflowRunStatus.Failed,
                            StepResults = stepResults,
                            FinalOutput = null,
                            ErrorMessage = stepResult.ErrorMessage,
                            StartedAt = startedAt,
                            CompletedAt = DateTime.UtcNow
                        };

                        await PersistResultAsync(result);
                        return result;
                    }

                    if (stepResult.Status == "suspended")
                    {
                        _logger.Info("Workflow '{0}' suspended at step '{1}'", Id, step.Id);

                        result = new WorkflowResult
                        {
                            RunId = runId,
                            Status = WorkflowRunStatus.Suspended,
                            StepResults = stepResults,
                            FinalOutput = stepResult.Output,
                            StartedAt = startedAt,
                            CompletedAt = DateTime.UtcNow
                        };

                        await PersistResultAsync(result);
                        return result;
                    }

                    // Pass step output as next step's input
                    currentInput = stepResult.Output;
                }

                _logger.Info("Workflow '{0}' completed successfully (runId={1})", Id, runId);

                result = new WorkflowResult
                {
                    RunId = runId,
                    Status = WorkflowRunStatus.Completed,
                    StepResults = stepResults,
                    FinalOutput = currentInput,
                    StartedAt = startedAt,
                    CompletedAt = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                _logger.Error("Workflow '{0}' failed unexpectedly: {1}", Id, ex.Message);

                result = new WorkflowResult
                {
                    RunId = runId,
                    Status = WorkflowRunStatus.Failed,
                    StepResults = stepResults,
                    ErrorMessage = ex.Message,
                    StartedAt = startedAt,
                    CompletedAt = DateTime.UtcNow
                };
            }

            await PersistResultAsync(result);
            return result;
        }

        private async Task PersistResultAsync(WorkflowResult workflowResult)
        {
            if (_storage == null) return;

            await _storage.SaveWorkflowRunAsync(new WorkflowRun
            {
                RunId = workflowResult.RunId,
                WorkflowId = Id,
                Status = workflowResult.Status,
                CreatedAt = workflowResult.StartedAt,
                UpdatedAt = workflowResult.CompletedAt,
                Output = workflowResult.FinalOutput != null
                    ? new Dictionary<string, object> { { "data", workflowResult.FinalOutput } }
                    : null,
                ErrorMessage = workflowResult.ErrorMessage
            });
        }
    }

    /// <summary>
    /// Factory helper mirroring createWorkflow() from packages/core/src/workflows/workflow.ts
    /// </summary>
    public static class WorkflowFactory
    {
        public static Workflow CreateWorkflow(string id, string description = null)
        {
            return new Workflow(id, description);
        }
    }
}
