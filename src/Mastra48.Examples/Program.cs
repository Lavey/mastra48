using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Mastra48;
using Mastra48.Agent;
using Mastra48.Events;
using Mastra48.Llm;
using Mastra48.Logger;
using Mastra48.Memory;
using Mastra48.Storage;
using Mastra48.Tools;
using Mastra48.Workflows;

namespace Mastra48.Examples
{
    // -------------------------------------------------------------------------
    // Stub language model used by examples (no real HTTP calls needed)
    // -------------------------------------------------------------------------

    internal class EchoLanguageModel : IMastraLanguageModel
    {
        public string ModelId => "echo/v1";
        public string Provider => "echo";

        public Task<GenerateTextResult> GenerateTextAsync(GenerateOptions options)
        {
            var lastMessage = options.Messages != null && options.Messages.Count > 0
                ? options.Messages[options.Messages.Count - 1].Content
                : string.Empty;

            return Task.FromResult(new GenerateTextResult
            {
                Text = $"[echo] {lastMessage}",
                FinishReason = "stop",
                PromptTokens = 10,
                CompletionTokens = 5
            });
        }
    }

    // -------------------------------------------------------------------------
    // Entry point
    // -------------------------------------------------------------------------

    internal static class Program
    {
        private const string MemoryThreadId   = "thread-demo";
        private const string MemoryResourceId = "user-1";
        private static async Task Main(string[] args)
        {
            Console.WriteLine("=== Mastra48 Examples ===");
            Console.WriteLine();

            await RunAgentExampleAsync();
            await RunToolExampleAsync();
            await RunWorkflowExampleAsync();
            await RunMemoryExampleAsync();
            await RunEventsExampleAsync();
            RunLoggerExample();

            Console.WriteLine();
            Console.WriteLine("All examples completed successfully.");
        }

        // =====================================================================
        // Example 1 – Basic Agent
        // =====================================================================

        /// <summary>
        /// Demonstrates creating a Mastra instance with an agent backed by a
        /// stub language model and generating a text response.
        ///
        /// Mirrors the "hello world" agent pattern from the original mastra-ai repo.
        /// </summary>
        private static async Task RunAgentExampleAsync()
        {
            Console.WriteLine("--- Example 1: Basic Agent ---");

            // Create the agent
            var agent = new Agent.Agent(new AgentConfig
            {
                Id = "hello-agent",
                Name = "Hello Agent",
                Instructions = "You are a friendly assistant that greets users.",
                Model = new ModelConfig { ModelId = "echo/v1" }
            });

            // Inject a stub language model (in production use a real provider)
            agent.SetLanguageModel(new EchoLanguageModel());

            // Register the agent in a Mastra instance
            var mastra = new Mastra(new MastraConfig
            {
                Agents = new Dictionary<string, Agent.Agent>
                {
                    { "helloAgent", agent }
                },
                Logger = new ConsoleLogger("Example1", LogLevel.WARN)
            });

            // Generate a response
            var result = await mastra.GetAgent("helloAgent")
                .GenerateAsync(new AgentGenerateOptions { Prompt = "Hello, world!" });

            Console.WriteLine($"  Response : {result.Text}");
            Console.WriteLine($"  Tokens   : {result.PromptTokens} prompt / {result.CompletionTokens} completion");
            Console.WriteLine();
        }

        // =====================================================================
        // Example 2 – Tools
        // =====================================================================

        /// <summary>
        /// Demonstrates creating a typed tool with ToolFactory and calling it
        /// directly via ExecuteAsync.
        ///
        /// Mirrors createTool() from packages/core/src/tools/tool.ts.
        /// </summary>
        private static async Task RunToolExampleAsync()
        {
            Console.WriteLine("--- Example 2: Tools ---");

            // Create a tool that greets a user by name
            var greetTool = ToolFactory.CreateTool<string, string>(
                id: "greet",
                description: "Greets the user by name",
                execute: async (name, ctx) =>
                {
                    await Task.Yield();
                    return $"Hello, {name}! How can I assist you today?";
                });

            // Create a calculation tool
            var addTool = ToolFactory.CreateTool<int[], int>(
                id: "add",
                description: "Adds two integers",
                execute: async (numbers, ctx) =>
                {
                    await Task.Yield();
                    return numbers[0] + numbers[1];
                });

            // Register tools in a Mastra instance
            var mastra = new Mastra(new MastraConfig
            {
                Tools = new Dictionary<string, IToolAction>
                {
                    { "greet", greetTool },
                    { "add",   addTool }
                },
                Logger = new ConsoleLogger("Example2", LogLevel.WARN)
            });

            // Execute the greet tool
            var greeting = await greetTool.ExecuteAsync("Alice");
            Console.WriteLine($"  greet(\"Alice\") => {greeting}");

            // Execute the add tool
            var sum = await addTool.ExecuteAsync(new[] { 3, 7 });
            Console.WriteLine($"  add([3, 7])    => {sum}");

            // Look up a tool from Mastra and verify it is the same instance
            var lookedUp = mastra.GetTool("greet");
            Console.WriteLine($"  GetTool(\"greet\").Id => {lookedUp.Id}");
            Console.WriteLine();
        }

        // =====================================================================
        // Example 3 – Workflow
        // =====================================================================

        /// <summary>
        /// Demonstrates building a multi-step workflow with WorkflowFactory,
        /// running it with initial input, and inspecting per-step results.
        ///
        /// Mirrors .then() chaining from packages/core/src/workflows/workflow.ts.
        /// </summary>
        private static async Task RunWorkflowExampleAsync()
        {
            Console.WriteLine("--- Example 3: Workflow ---");

            // Step 1: validate the input
            var validateStep = StepFactory.CreateStep(
                id: "validate",
                description: "Validates the order input",
                execute: async ctx =>
                {
                    await Task.Yield();
                    var input = ctx.Input?.ToString() ?? string.Empty;
                    if (string.IsNullOrWhiteSpace(input))
                        throw new InvalidOperationException("Input cannot be empty.");
                    return (object)$"validated:{input}";
                });

            // Step 2: process the validated data
            var processStep = StepFactory.CreateStep(
                id: "process",
                description: "Processes the validated order",
                execute: async ctx =>
                {
                    await Task.Yield();
                    var previous = ctx.PreviousStepResults != null &&
                                   ctx.PreviousStepResults.ContainsKey("validate")
                        ? ctx.PreviousStepResults["validate"].Output?.ToString()
                        : string.Empty;
                    return (object)$"processed:{previous}";
                });

            // Step 3: notify completion
            var notifyStep = StepFactory.CreateStep(
                id: "notify",
                description: "Sends a completion notification",
                execute: async ctx =>
                {
                    await Task.Yield();
                    return (object)"notification sent";
                });

            // Build and register the workflow
            var workflow = WorkflowFactory.CreateWorkflow("order-pipeline", "Order processing pipeline")
                .Then(validateStep)
                .Then(processStep)
                .Then(notifyStep);

            var mastra = new Mastra(new MastraConfig
            {
                Workflows = new Dictionary<string, Workflow>
                {
                    { "orderPipeline", workflow }
                },
                Logger = new ConsoleLogger("Example3", LogLevel.WARN)
            });

            // Run the workflow
            var result = await mastra.GetWorkflow("orderPipeline")
                .RunAsync(new WorkflowRunOptions { Input = "ORDER-42" });

            Console.WriteLine($"  Status       : {result.Status}");
            Console.WriteLine($"  FinalOutput  : {result.FinalOutput}");
            foreach (var step in result.StepResults)
                Console.WriteLine($"  Step [{step.Key}] => {step.Value.Output}");
            Console.WriteLine();
        }

        // =====================================================================
        // Example 4 – Memory
        // =====================================================================

        /// <summary>
        /// Demonstrates using InMemoryMemory for conversational context.
        /// Messages saved to a thread are retrieved in subsequent calls.
        ///
        /// Mirrors the memory pattern from packages/core/src/memory.
        /// </summary>
        private static async Task RunMemoryExampleAsync()
        {
            Console.WriteLine("--- Example 4: Memory ---");

            var memory = new InMemoryMemory(new MemoryConfig { LastMessages = 10 });

            // Ensure the thread exists
            await memory.GetOrCreateThreadAsync(MemoryResourceId, MemoryThreadId, "Demo conversation");

            // Save a user message and an assistant reply
            // StorageMessage.Id is required by the IMastraStorage contract
            await memory.SaveMessagesAsync(MemoryThreadId, new List<StorageMessage>
            {
                new StorageMessage
                {
                    Id = Guid.NewGuid().ToString(),
                    ThreadId = MemoryThreadId,
                    ResourceId = MemoryResourceId,
                    Role = "user",
                    Content = "What is the capital of Poland?",
                    CreatedAt = DateTime.UtcNow
                },
                new StorageMessage
                {
                    Id = Guid.NewGuid().ToString(),
                    ThreadId = MemoryThreadId,
                    ResourceId = MemoryResourceId,
                    Role = "assistant",
                    Content = "The capital of Poland is Warsaw.",
                    CreatedAt = DateTime.UtcNow
                }
            });

            // Retrieve the conversation history
            var messages = await memory.GetMessagesAsync(MemoryThreadId, MemoryResourceId);
            Console.WriteLine($"  Messages in thread '{MemoryThreadId}':");
            foreach (var msg in messages)
                Console.WriteLine($"    [{msg.Role}] {msg.Content}");

            // Delete the thread
            await memory.DeleteThreadAsync(MemoryThreadId);
            var afterDelete = await memory.GetMessagesAsync(MemoryThreadId, MemoryResourceId);
            Console.WriteLine($"  Messages after deletion: {afterDelete.Count}");
            Console.WriteLine();
        }

        // =====================================================================
        // Example 5 – PubSub / Events
        // =====================================================================

        /// <summary>
        /// Demonstrates subscribing to topics and publishing events through the
        /// Mastra pub/sub system.
        ///
        /// Mirrors EventEmitterPubSub from packages/core/src/events/event-emitter.ts.
        /// </summary>
        private static async Task RunEventsExampleAsync()
        {
            Console.WriteLine("--- Example 5: Events / PubSub ---");

            var received = new List<string>();

            var mastra = new Mastra(new MastraConfig
            {
                Events = new Dictionary<string, Events.EventHandler>
                {
                    {
                        "order.created",
                        async (evt, next) =>
                        {
                            received.Add($"order.created:{evt.Payload}");
                            await next();
                        }
                    },
                    {
                        "order.shipped",
                        async (evt, next) =>
                        {
                            received.Add($"order.shipped:{evt.Payload}");
                            await next();
                        }
                    }
                },
                Logger = new ConsoleLogger("Example5", LogLevel.WARN)
            });

            // Publish events
            await mastra.PublishEventAsync("order.created", new MastraEvent
            {
                Topic   = "order.created",
                Payload = "ORDER-42",
                Timestamp = DateTime.UtcNow
            });

            await mastra.PublishEventAsync("order.shipped", new MastraEvent
            {
                Topic   = "order.shipped",
                Payload = "ORDER-42",
                Timestamp = DateTime.UtcNow
            });

            Console.WriteLine($"  Events received ({received.Count}):");
            foreach (var e in received)
                Console.WriteLine($"    {e}");
            Console.WriteLine();
        }

        // =====================================================================
        // Example 6 – Logger
        // =====================================================================

        /// <summary>
        /// Demonstrates the built-in logger implementations:
        /// ConsoleLogger (writes to stdout) and NoopLogger (silent).
        ///
        /// Mirrors the logging abstraction from packages/core/src/logger.
        /// </summary>
        private static void RunLoggerExample()
        {
            Console.WriteLine("--- Example 6: Logger ---");

            // ConsoleLogger – writes formatted lines to stdout
            var consoleLogger = new ConsoleLogger("MyApp", LogLevel.DEBUG);
            consoleLogger.Debug("Debug message (most verbose)");
            consoleLogger.Info("Application started");
            consoleLogger.Warn("Low memory warning");
            consoleLogger.Error("Something went wrong: {0}", "disk full");

            // NoopLogger – discards all log entries (useful in tests / production silence)
            var noopLogger = NoopLogger.Instance;
            noopLogger.Info("This message is silently discarded.");

            Console.WriteLine($"  ConsoleLogger type : {consoleLogger.GetType().Name}");
            Console.WriteLine($"  NoopLogger type    : {noopLogger.GetType().Name}");
            Console.WriteLine();
        }
    }
}
