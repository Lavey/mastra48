using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using Aype.AI;
using Aype.AI.Agent;
using Aype.AI.Error;
using Aype.AI.Events;
using Aype.AI.Llm;
using Aype.AI.Logger;
using Aype.AI.Memory;
using Aype.AI.Storage;
using Aype.AI.Tools;
using Aype.AI.Workflows;

namespace Aype.AI.Tests
{
    // =========================================================
    // Fake language model for tests (no real HTTP calls)
    // =========================================================

    internal class FakeLanguageModel : IMastraLanguageModel
    {
        public string ModelId => "fake/gpt-test";
        public string Provider => "fake";

        private readonly string _responseText;

        public FakeLanguageModel(string responseText = "Hello from fake model!")
        {
            _responseText = responseText;
        }

        public Task<GenerateTextResult> GenerateTextAsync(GenerateOptions options)
        {
            return Task.FromResult(new GenerateTextResult
            {
                Text = _responseText,
                FinishReason = "stop",
                PromptTokens = 10,
                CompletionTokens = 5
            });
        }
    }

    // =========================================================
    // Error tests
    // =========================================================

    [TestFixture]
    public class ErrorTests
    {
        [Test]
        public void MastraError_HasCorrectDomainAndCategory()
        {
            var error = new MastraError(new ErrorDefinition
            {
                Id = "TEST_ERROR",
                Domain = ErrorDomain.AGENT,
                Category = ErrorCategory.USER,
                Text = "Test error message"
            });

            Assert.AreEqual("TEST_ERROR", error.Id);
            Assert.AreEqual(ErrorDomain.AGENT, error.Domain);
            Assert.AreEqual(ErrorCategory.USER, error.Category);
            Assert.AreEqual("Test error message", error.Message);
        }

        [Test]
        public void MastraError_ToJson_ContainsExpectedFields()
        {
            var error = new MastraError(new ErrorDefinition
            {
                Id = "JSON_TEST",
                Domain = ErrorDomain.TOOL,
                Category = ErrorCategory.SYSTEM,
                Text = "JSON test"
            });

            var json = error.ToJson();

            Assert.AreEqual("JSON_TEST", json.Code);
            Assert.AreEqual("JSON test", json.Message);
            Assert.AreEqual("TOOL", json.Domain);
            Assert.AreEqual("SYSTEM", json.Category);
        }

        [Test]
        public void MastraError_WithInnerException_UsesInnerMessage()
        {
            var inner = new Exception("inner message");
            var error = new MastraError(new ErrorDefinition
            {
                Id = "INNER_ERR",
                Domain = ErrorDomain.MASTRA,
                Category = ErrorCategory.THIRD_PARTY
            }, inner);

            Assert.AreEqual("inner message", error.Message);
            Assert.IsNotNull(error.InnerException);
        }
    }

    // =========================================================
    // Logger tests
    // =========================================================

    [TestFixture]
    public class LoggerTests
    {
        [Test]
        public void NoopLogger_DoesNotThrow()
        {
            var logger = NoopLogger.Instance;

            Assert.DoesNotThrow(() => logger.Debug("debug message"));
            Assert.DoesNotThrow(() => logger.Info("info message"));
            Assert.DoesNotThrow(() => logger.Warn("warn message"));
            Assert.DoesNotThrow(() => logger.Error("error message"));
        }

        [Test]
        public void ConsoleLogger_DoesNotThrow()
        {
            var logger = new ConsoleLogger("TestApp", LogLevel.DEBUG);

            Assert.DoesNotThrow(() => logger.Debug("debug"));
            Assert.DoesNotThrow(() => logger.Info("info"));
            Assert.DoesNotThrow(() => logger.Warn("warn"));
            Assert.DoesNotThrow(() => logger.Error("error"));
        }

        [Test]
        public async Task NoopLogger_ListLogs_ReturnsEmpty()
        {
            var logger = NoopLogger.Instance;
            var result = await logger.ListLogs("nonexistent", new LogQueryParams());

            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Total);
            Assert.AreEqual(0, result.Logs.Count);
            Assert.IsFalse(result.HasMore);
        }
    }

    // =========================================================
    // Storage tests
    // =========================================================

    [TestFixture]
    public class StorageTests
    {
        private InMemoryStorage _storage;

        [SetUp]
        public void SetUp()
        {
            _storage = new InMemoryStorage();
        }

        [Test]
        public async Task SaveAndGetWorkflowRun()
        {
            var run = new WorkflowRun
            {
                RunId = "run-1",
                WorkflowId = "wf-1",
                Status = WorkflowRunStatus.Running,
                CreatedAt = DateTime.UtcNow
            };

            await _storage.SaveWorkflowRunAsync(run);
            var retrieved = await _storage.GetWorkflowRunByIdAsync("run-1");

            Assert.IsNotNull(retrieved);
            Assert.AreEqual("wf-1", retrieved.WorkflowId);
            Assert.AreEqual(WorkflowRunStatus.Running, retrieved.Status);
        }

        [Test]
        public async Task DeleteWorkflowRun_RemovesIt()
        {
            var run = new WorkflowRun
            {
                RunId = "run-del",
                WorkflowId = "wf-del",
                Status = WorkflowRunStatus.Completed,
                CreatedAt = DateTime.UtcNow
            };

            await _storage.SaveWorkflowRunAsync(run);
            await _storage.DeleteWorkflowRunAsync("run-del");

            var retrieved = await _storage.GetWorkflowRunByIdAsync("run-del");
            Assert.IsNull(retrieved);
        }

        [Test]
        public async Task SaveAndGetThread()
        {
            var thread = new StorageThread
            {
                Id = "thread-1",
                ResourceId = "user-1",
                Title = "Test Thread",
                CreatedAt = DateTime.UtcNow
            };

            await _storage.SaveThreadAsync(thread);
            var retrieved = await _storage.GetThreadByIdAsync("thread-1");

            Assert.IsNotNull(retrieved);
            Assert.AreEqual("user-1", retrieved.ResourceId);
            Assert.AreEqual("Test Thread", retrieved.Title);
        }

        [Test]
        public async Task SaveAndGetMessages()
        {
            var messages = new List<StorageMessage>
            {
                new StorageMessage { Id = "msg-1", ThreadId = "t1", Role = "user", Content = "Hello", CreatedAt = DateTime.UtcNow },
                new StorageMessage { Id = "msg-2", ThreadId = "t1", Role = "assistant", Content = "Hi there", CreatedAt = DateTime.UtcNow }
            };

            await _storage.SaveMessagesAsync(messages);
            var retrieved = await _storage.GetMessagesByThreadIdAsync("t1");

            Assert.AreEqual(2, retrieved.Count);
        }

        [Test]
        public async Task ListWorkflowRuns_FiltersByWorkflowId()
        {
            await _storage.SaveWorkflowRunAsync(new WorkflowRun { RunId = "r1", WorkflowId = "wf-a", Status = WorkflowRunStatus.Completed, CreatedAt = DateTime.UtcNow });
            await _storage.SaveWorkflowRunAsync(new WorkflowRun { RunId = "r2", WorkflowId = "wf-b", Status = WorkflowRunStatus.Completed, CreatedAt = DateTime.UtcNow });

            var runs = await _storage.ListWorkflowRunsAsync(new ListWorkflowRunsInput { WorkflowId = "wf-a" });

            Assert.AreEqual(1, runs.Count);
            Assert.AreEqual("r1", runs[0].RunId);
        }
    }

    // =========================================================
    // Memory tests
    // =========================================================

    [TestFixture]
    public class MemoryTests
    {
        [Test]
        public async Task InMemoryMemory_GetOrCreateThread_CreatesThread()
        {
            var memory = new InMemoryMemory();
            var thread = await memory.GetOrCreateThreadAsync("user-1", title: "My Thread");

            Assert.IsNotNull(thread);
            Assert.AreEqual("user-1", thread.ResourceId);
            Assert.AreEqual("My Thread", thread.Title);
        }

        [Test]
        public async Task InMemoryMemory_GetOrCreateThread_ReturnsExisting()
        {
            var memory = new InMemoryMemory();
            var created = await memory.GetOrCreateThreadAsync("user-1", "thread-x");
            var retrieved = await memory.GetOrCreateThreadAsync("user-1", "thread-x");

            Assert.AreEqual(created.Id, retrieved.Id);
        }

        [Test]
        public async Task InMemoryMemory_SaveAndGetMessages()
        {
            var memory = new InMemoryMemory();
            var thread = await memory.GetOrCreateThreadAsync("user-1");

            var messages = new List<StorageMessage>
            {
                new StorageMessage { Id = "m1", ThreadId = thread.Id, Role = "user", Content = "Ping", CreatedAt = DateTime.UtcNow },
                new StorageMessage { Id = "m2", ThreadId = thread.Id, Role = "assistant", Content = "Pong", CreatedAt = DateTime.UtcNow }
            };

            await memory.SaveMessagesAsync(thread.Id, messages);
            var retrieved = await memory.GetMessagesAsync(thread.Id);

            Assert.AreEqual(2, retrieved.Count);
        }

        [Test]
        public async Task InMemoryMemory_LastMessages_LimitsResults()
        {
            var memory = new InMemoryMemory(new MemoryConfig { LastMessages = 1 });
            var thread = await memory.GetOrCreateThreadAsync("user-1");

            var messages = new List<StorageMessage>
            {
                new StorageMessage { Id = "m1", ThreadId = thread.Id, Role = "user", Content = "Msg1", CreatedAt = DateTime.UtcNow },
                new StorageMessage { Id = "m2", ThreadId = thread.Id, Role = "assistant", Content = "Msg2", CreatedAt = DateTime.UtcNow }
            };

            await memory.SaveMessagesAsync(thread.Id, messages);
            var retrieved = await memory.GetMessagesAsync(thread.Id);

            Assert.AreEqual(1, retrieved.Count);
            Assert.AreEqual("m2", retrieved[0].Id);
        }
    }

    // =========================================================
    // Tool tests
    // =========================================================

    [TestFixture]
    public class ToolTests
    {
        [Test]
        public async Task Tool_ExecuteAsync_ReturnsExpectedOutput()
        {
            var tool = new Tool<string, string>(
                id: "reverse",
                description: "Reverses a string",
                execute: async (input, ctx) =>
                {
                    var chars = input.ToCharArray();
                    Array.Reverse(chars);
                    return await Task.FromResult(new string(chars));
                });

            var result = await tool.ExecuteAsync("hello");
            Assert.AreEqual("olleh", result);
        }

        [Test]
        public void Tool_NullId_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() =>
                new Tool<string, string>(null, "desc",
                    async (i, c) => await Task.FromResult(i)));
        }

        [Test]
        public void Tool_NullExecute_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new Tool<string, string>("my-tool", "desc", null));
        }

        [Test]
        public async Task ToolFactory_CreateTool_Works()
        {
            var tool = ToolFactory.CreateTool<int, int>(
                "double",
                "Doubles a number",
                async (n, ctx) => await Task.FromResult(n * 2));

            var result = await tool.ExecuteAsync(5);
            Assert.AreEqual(10, result);
        }
    }

    // =========================================================
    // Workflow tests
    // =========================================================

    [TestFixture]
    public class WorkflowTests
    {
        [Test]
        public async Task Workflow_RunsStepsSequentially()
        {
            var order = new List<string>();

            var step1 = StepFactory.CreateStep("step1",
                async ctx => { order.Add("step1"); return await Task.FromResult((object)"s1"); });

            var step2 = StepFactory.CreateStep("step2",
                async ctx => { order.Add("step2"); return await Task.FromResult((object)"s2"); });

            var workflow = WorkflowFactory.CreateWorkflow("test-wf")
                .Then(step1)
                .Then(step2);

            var result = await workflow.RunAsync();

            Assert.AreEqual(WorkflowRunStatus.Completed, result.Status);
            Assert.AreEqual(new List<string> { "step1", "step2" }, order);
            Assert.AreEqual("s2", result.FinalOutput);
        }

        [Test]
        public async Task Workflow_FailedStep_StopsExecution()
        {
            var step1 = StepFactory.CreateStep("step1",
                ctx => { throw new Exception("boom"); });

            var step2 = StepFactory.CreateStep("step2",
                async ctx => await Task.FromResult((object)"never"));

            var workflow = WorkflowFactory.CreateWorkflow("fail-wf")
                .Then(step1)
                .Then(step2);

            var result = await workflow.RunAsync();

            Assert.AreEqual(WorkflowRunStatus.Failed, result.Status);
            Assert.AreEqual("boom", result.ErrorMessage);
            Assert.IsFalse(result.StepResults.ContainsKey("step2"));
        }

        [Test]
        public async Task Workflow_PassesOutputBetweenSteps()
        {
            var step1 = StepFactory.CreateStep("step1",
                async ctx => await Task.FromResult((object)42));

            var step2 = StepFactory.CreateStep("step2",
                async ctx =>
                {
                    var val = (int)ctx.Input;
                    return await Task.FromResult((object)(val * 2));
                });

            var workflow = WorkflowFactory.CreateWorkflow("chain-wf")
                .Then(step1)
                .Then(step2);

            var result = await workflow.RunAsync();

            Assert.AreEqual(WorkflowRunStatus.Completed, result.Status);
            Assert.AreEqual(84, result.FinalOutput);
        }

        [Test]
        public async Task Workflow_PersistsRunToStorage()
        {
            var storage = new InMemoryStorage();

            var step = StepFactory.CreateStep("s1",
                async ctx => await Task.FromResult((object)"done"));

            var workflow = WorkflowFactory.CreateWorkflow("stored-wf").Then(step);
            workflow.SetStorage(storage);

            var result = await workflow.RunAsync(new WorkflowRunOptions { RunId = "fixed-id" });

            var saved = await storage.GetWorkflowRunByIdAsync("fixed-id");
            Assert.IsNotNull(saved);
            Assert.AreEqual(WorkflowRunStatus.Completed, saved.Status);
        }

        [Test]
        public void Workflow_NullId_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => new Workflow(null));
        }
    }

    // =========================================================
    // Agent tests
    // =========================================================

    [TestFixture]
    public class AgentTests
    {
        [Test]
        public async Task Agent_GenerateAsync_ReturnsModelResponse()
        {
            var agent = new Agent.Agent(new AgentConfig
            {
                Name = "TestAgent",
                Instructions = "You are a test assistant",
                Model = new ModelConfig { ModelId = "fake/model" }
            });
            agent.SetLanguageModel(new FakeLanguageModel("Hello!"));

            var result = await agent.GenerateAsync(new AgentGenerateOptions { Prompt = "Hi" });

            Assert.AreEqual("Hello!", result.Text);
            Assert.AreEqual("stop", result.FinishReason);
        }

        [Test]
        public void Agent_NoModel_ThrowsMastraError()
        {
            var agent = new Agent.Agent(new AgentConfig
            {
                Name = "NoModelAgent",
                Instructions = "Test",
                Model = new ModelConfig { ModelId = "fake/model" }
            });
            // Do NOT call SetLanguageModel()

            Assert.ThrowsAsync<MastraError>(async () =>
                await agent.GenerateAsync(new AgentGenerateOptions { Prompt = "Hello" }));
        }

        [Test]
        public void Agent_NullConfig_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new Agent.Agent(null));
        }

        [Test]
        public void Agent_EmptyName_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => new Agent.Agent(new AgentConfig { Name = "" }));
        }

        [Test]
        public async Task Agent_WithMemory_PersistsMessages()
        {
            var memory = new InMemoryMemory();
            var thread = await memory.GetOrCreateThreadAsync("user-1", "thread-1");

            var agent = new Agent.Agent(new AgentConfig
            {
                Name = "MemoryAgent",
                Instructions = "Test",
                Model = new ModelConfig { ModelId = "fake/model" },
                Memory = memory
            });
            agent.SetLanguageModel(new FakeLanguageModel("Remembered!"));

            await agent.GenerateAsync(new AgentGenerateOptions
            {
                Prompt = "Remember me",
                ThreadId = thread.Id,
                ResourceId = "user-1"
            });

            var messages = await memory.GetMessagesAsync(thread.Id);
            Assert.AreEqual(2, messages.Count);
            Assert.AreEqual("user", messages[0].Role);
            Assert.AreEqual("Remember me", messages[0].Content);
            Assert.AreEqual("assistant", messages[1].Role);
            Assert.AreEqual("Remembered!", messages[1].Content);
        }
    }

    // =========================================================
    // Mastra orchestrator tests
    // =========================================================

    [TestFixture]
    public class MastraTests
    {
        [Test]
        public void Mastra_DefaultConfig_InitializesSuccessfully()
        {
            var mastra = new Mastra();
            Assert.IsNotNull(mastra.GetLogger());
            Assert.IsNotNull(mastra.GetStorage());
            Assert.IsNotNull(mastra.GetPubSub());
        }

        [Test]
        public void Mastra_GetAgent_AfterAddAgent_ReturnsIt()
        {
            var agent = new Agent.Agent(new AgentConfig
            {
                Name = "TestAgent",
                Instructions = "Test",
                Model = new ModelConfig { ModelId = "fake/model" }
            });

            var mastra = new Mastra();
            mastra.AddAgent("test", agent);

            var retrieved = mastra.GetAgent("test");
            Assert.AreSame(agent, retrieved);
        }

        [Test]
        public void Mastra_GetAgent_NotFound_ThrowsMastraError()
        {
            var mastra = new Mastra();
            Assert.Throws<MastraError>(() => mastra.GetAgent("nonexistent"));
        }

        [Test]
        public void Mastra_GetWorkflow_AfterAddWorkflow_ReturnsIt()
        {
            var workflow = new Workflow("test-wf");
            var mastra = new Mastra();
            mastra.AddWorkflow("test", workflow);

            var retrieved = mastra.GetWorkflow("test");
            Assert.AreSame(workflow, retrieved);
        }

        [Test]
        public void Mastra_GetWorkflow_NotFound_ThrowsMastraError()
        {
            var mastra = new Mastra();
            Assert.Throws<MastraError>(() => mastra.GetWorkflow("nonexistent"));
        }

        [Test]
        public void Mastra_ConfigWithAgentsAndWorkflows_RegistersAll()
        {
            var agent = new Agent.Agent(new AgentConfig
            {
                Name = "Agent1",
                Instructions = "Test",
                Model = new ModelConfig { ModelId = "fake/model" }
            });

            var workflow = new Workflow("wf-1");

            var mastra = new Mastra(new MastraConfig
            {
                Agents = new Dictionary<string, Agent.Agent> { { "agent1", agent } },
                Workflows = new Dictionary<string, Workflow> { { "workflow1", workflow } },
                Logger = NoopLogger.Instance
            });

            Assert.AreEqual(1, mastra.GetAgents().Count);
            Assert.AreEqual(1, mastra.GetWorkflows().Count);
        }

        [Test]
        public void Mastra_GenerateId_ReturnsUniqueStrings()
        {
            var mastra = new Mastra();
            var id1 = mastra.GenerateId();
            var id2 = mastra.GenerateId();

            Assert.IsNotEmpty(id1);
            Assert.IsNotEmpty(id2);
            Assert.AreNotEqual(id1, id2);
        }

        [Test]
        public async Task Mastra_PublishEvent_ReachesSubscribers()
        {
            var received = new List<string>();

            var mastra = new Mastra(new MastraConfig
            {
                Events = new Dictionary<string, Events.EventHandler>
                {
                    {
                        "test.topic",
                        async (evt, cb) =>
                        {
                            received.Add(evt.Topic);
                            await cb();
                        }
                    }
                }
            });

            await mastra.PublishEventAsync("test.topic", new MastraEvent
            {
                Topic = "test.topic",
                Payload = new { data = 42 },
                Timestamp = DateTime.UtcNow
            });

            Assert.AreEqual(1, received.Count);
            Assert.AreEqual("test.topic", received[0]);
        }

        [Test]
        public void Mastra_AddAgent_NullKey_ThrowsMastraError()
        {
            var mastra = new Mastra();
            var agent = new Agent.Agent(new AgentConfig
            {
                Name = "A",
                Instructions = "T",
                Model = new ModelConfig { ModelId = "f" }
            });
            Assert.Throws<MastraError>(() => mastra.AddAgent(null, agent));
        }

        [Test]
        public void Mastra_AddAgent_NullAgent_ThrowsMastraError()
        {
            var mastra = new Mastra();
            Assert.Throws<MastraError>(() => mastra.AddAgent("key", null));
        }
    }

    // =========================================================
    // PubSub tests
    // =========================================================

    [TestFixture]
    public class PubSubTests
    {
        [Test]
        public async Task EventEmitterPubSub_PublishesToSubscribers()
        {
            var pubSub = new EventEmitterPubSub();
            var results = new List<string>();

            pubSub.Subscribe("topic.a", async (evt, cb) =>
            {
                results.Add(evt.Topic);
                await cb();
            });

            await pubSub.PublishAsync("topic.a", new MastraEvent { Topic = "topic.a" });

            Assert.AreEqual(1, results.Count);
        }

        [Test]
        public async Task EventEmitterPubSub_NoSubscribers_DoesNotThrow()
        {
            var pubSub = new EventEmitterPubSub();
            Assert.DoesNotThrowAsync(async () =>
                await pubSub.PublishAsync("no.subscribers", new MastraEvent { Topic = "no.subscribers" }));
        }

        [Test]
        public async Task EventEmitterPubSub_Unsubscribe_StopsDelivery()
        {
            var pubSub = new EventEmitterPubSub();
            var count = 0;

            Events.EventHandler handler = async (evt, cb) =>
            {
                count++;
                await cb();
            };

            pubSub.Subscribe("topic.b", handler);
            await pubSub.PublishAsync("topic.b", new MastraEvent { Topic = "topic.b" });
            Assert.AreEqual(1, count);

            pubSub.Unsubscribe("topic.b", handler);
            await pubSub.PublishAsync("topic.b", new MastraEvent { Topic = "topic.b" });
            Assert.AreEqual(1, count);
        }
    }
}
