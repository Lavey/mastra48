using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Aype.AI.Agent;
using Aype.AI.Error;
using Aype.AI.Events;
using Aype.AI.Logger;
using Aype.AI.Memory;
using Aype.AI.Storage;
using Aype.AI.Tools;
using Aype.AI.Workflows;

namespace Aype.AI
{
    /// <summary>
    /// Configuration interface for initializing a Mastra instance.
    ///
    /// Mirrors Config&lt;...&gt; from packages/core/src/mastra/index.ts
    /// </summary>
    public class MastraConfig
    {
        /// <summary>
        /// Agents are autonomous systems that can make decisions and take actions.
        /// </summary>
        public Dictionary<string, Agent.Agent> Agents { get; set; }

        /// <summary>
        /// Storage provider for persisting data, conversation history, and workflow state.
        /// Required for agent memory and workflow persistence.
        /// </summary>
        public IMastraStorage Storage { get; set; }

        /// <summary>
        /// Logger implementation for application logging and debugging.
        /// Set to null to use the default ConsoleLogger.
        /// </summary>
        public IMastraLogger Logger { get; set; }

        /// <summary>
        /// Workflows provide composable task execution with built-in error handling.
        /// </summary>
        public Dictionary<string, Workflow> Workflows { get; set; }

        /// <summary>
        /// Tools are reusable functions that agents can use to interact with external systems.
        /// </summary>
        public Dictionary<string, IToolAction> Tools { get; set; }

        /// <summary>
        /// Pub/sub system for event-driven communication between components.
        /// </summary>
        public IPubSub PubSub { get; set; }

        /// <summary>
        /// Custom ID generator function for creating unique identifiers.
        /// </summary>
        public Func<string> IdGenerator { get; set; }

        /// <summary>
        /// Memory instances that can be referenced by stored agents.
        /// </summary>
        public Dictionary<string, MastraMemory> Memory { get; set; }

        /// <summary>
        /// Event handlers for custom application events.
        /// Maps event topics to handler functions for event-driven architectures.
        /// </summary>
        public Dictionary<string, Events.EventHandler> Events { get; set; }
    }

    /// <summary>
    /// The central orchestrator for Mastra applications, managing agents,
    /// workflows, storage, logging, and more.
    ///
    /// Mirrors the Mastra class from packages/core/src/mastra/index.ts
    ///
    /// <example>
    /// <code>
    ///   var mastra = new Mastra(new MastraConfig
    ///   {
    ///       Agents = new Dictionary&lt;string, Agent&gt;
    ///       {
    ///           { "weatherAgent", new Agent(new AgentConfig
    ///             {
    ///                 Id = "weather-agent",
    ///                 Name = "Weather Agent",
    ///                 Instructions = "You help with weather information",
    ///                 Model = new ModelConfig { ModelId = "openai/gpt-4o" }
    ///             })
    ///           }
    ///       },
    ///       Storage = new InMemoryStorage(),
    ///       Logger = new ConsoleLogger("MyApp", LogLevel.INFO)
    ///   });
    ///
    ///   var agent = mastra.GetAgent("weatherAgent");
    ///   var result = await agent.GenerateAsync(new AgentGenerateOptions { Prompt = "What is the weather in Warsaw?" });
    /// </code>
    /// </example>
    /// </summary>
    public class Mastra
    {
        private readonly Dictionary<string, Agent.Agent> _agents;
        private readonly Dictionary<string, Workflow> _workflows;
        private readonly Dictionary<string, IToolAction> _tools;
        private readonly Dictionary<string, MastraMemory> _memory;
        private readonly IMastraStorage _storage;
        private readonly IMastraLogger _logger;
        private readonly IPubSub _pubSub;
        private readonly Func<string> _idGenerator;
        private readonly Dictionary<string, Events.EventHandler> _eventHandlers;

        /// <summary>
        /// Creates a new Mastra instance.
        /// </summary>
        public Mastra(MastraConfig config = null)
        {
            config = config ?? new MastraConfig();

            _logger = config.Logger ?? new ConsoleLogger("Mastra", LogLevel.INFO);
            _storage = config.Storage ?? new InMemoryStorage();
            _pubSub = config.PubSub ?? new EventEmitterPubSub();
            _idGenerator = config.IdGenerator ?? (() => Guid.NewGuid().ToString());
            _memory = config.Memory ?? new Dictionary<string, MastraMemory>();
            _eventHandlers = config.Events ?? new Dictionary<string, Events.EventHandler>();

            // Initialize agents
            _agents = new Dictionary<string, Agent.Agent>();
            if (config.Agents != null)
            {
                foreach (var kvp in config.Agents)
                {
                    if (kvp.Value == null)
                    {
                        _logger.Warn("Agent '{0}' is null and will be skipped.", kvp.Key);
                        continue;
                    }
                    AddAgent(kvp.Key, kvp.Value);
                }
            }

            // Initialize workflows
            _workflows = new Dictionary<string, Workflow>();
            if (config.Workflows != null)
            {
                foreach (var kvp in config.Workflows)
                {
                    if (kvp.Value == null)
                    {
                        _logger.Warn("Workflow '{0}' is null and will be skipped.", kvp.Key);
                        continue;
                    }
                    AddWorkflow(kvp.Key, kvp.Value);
                }
            }

            // Initialize tools
            _tools = new Dictionary<string, IToolAction>();
            if (config.Tools != null)
            {
                foreach (var kvp in config.Tools)
                {
                    if (kvp.Value == null)
                    {
                        _logger.Warn("Tool '{0}' is null and will be skipped.", kvp.Key);
                        continue;
                    }
                    _tools[kvp.Key] = kvp.Value;
                }
            }

            // Wire up event handlers to pub/sub
            foreach (var kvp in _eventHandlers)
                _pubSub.Subscribe(kvp.Key, kvp.Value);

            _logger.Info("Mastra initialized: {0} agent(s), {1} workflow(s), {2} tool(s)",
                _agents.Count, _workflows.Count, _tools.Count);
        }

        // =====================================================================
        // Agents
        // =====================================================================

        /// <summary>
        /// Registers an agent with the given key.
        /// Mirrors Mastra.addAgent() from packages/core/src/mastra/index.ts
        /// </summary>
        public void AddAgent(string key, Agent.Agent agent)
        {
            if (string.IsNullOrEmpty(key))
                throw CreateUndefinedError("agent", key);
            if (agent == null)
                throw CreateUndefinedError("agent", key);

            agent.SetLogger(_logger);
            _agents[key] = agent;
            _logger.Debug("Registered agent '{0}' (id={1})", key, agent.Id);
        }

        /// <summary>
        /// Returns the agent registered under the given key.
        /// Mirrors Mastra.getAgent() from packages/core/src/mastra/index.ts
        /// </summary>
        public Agent.Agent GetAgent(string key)
        {
            if (!_agents.ContainsKey(key))
                throw new MastraError(new ErrorDefinition
                {
                    Id = "AGENT_NOT_FOUND",
                    Domain = ErrorDomain.AGENT,
                    Category = ErrorCategory.USER,
                    Text = $"Agent '{key}' is not registered in this Mastra instance."
                });

            return _agents[key];
        }

        /// <summary>Returns all registered agents.</summary>
        public IReadOnlyDictionary<string, Agent.Agent> GetAgents()
            => _agents;

        // =====================================================================
        // Workflows
        // =====================================================================

        /// <summary>
        /// Registers a workflow with the given key.
        /// Mirrors Mastra.addWorkflow() from packages/core/src/mastra/index.ts
        /// </summary>
        public void AddWorkflow(string key, Workflow workflow)
        {
            if (string.IsNullOrEmpty(key))
                throw CreateUndefinedError("workflow", key);
            if (workflow == null)
                throw CreateUndefinedError("workflow", key);

            workflow.SetLogger(_logger);
            workflow.SetStorage(_storage);
            _workflows[key] = workflow;
            _logger.Debug("Registered workflow '{0}' (id={1})", key, workflow.Id);
        }

        /// <summary>
        /// Returns the workflow registered under the given key.
        /// Mirrors Mastra.getWorkflow() from packages/core/src/mastra/index.ts
        /// </summary>
        public Workflow GetWorkflow(string key)
        {
            if (!_workflows.ContainsKey(key))
                throw new MastraError(new ErrorDefinition
                {
                    Id = "WORKFLOW_NOT_FOUND",
                    Domain = ErrorDomain.MASTRA_WORKFLOW,
                    Category = ErrorCategory.USER,
                    Text = $"Workflow '{key}' is not registered in this Mastra instance."
                });

            return _workflows[key];
        }

        /// <summary>Returns all registered workflows.</summary>
        public IReadOnlyDictionary<string, Workflow> GetWorkflows()
            => _workflows;

        // =====================================================================
        // Tools
        // =====================================================================

        /// <summary>
        /// Returns a specific registered tool.
        /// Mirrors Mastra.getTool() from packages/core/src/mastra/index.ts
        /// </summary>
        public IToolAction GetTool(string key)
        {
            if (!_tools.ContainsKey(key))
                throw new MastraError(new ErrorDefinition
                {
                    Id = "TOOL_NOT_FOUND",
                    Domain = ErrorDomain.TOOL,
                    Category = ErrorCategory.USER,
                    Text = $"Tool '{key}' is not registered in this Mastra instance."
                });

            return _tools[key];
        }

        /// <summary>Returns all registered tools.</summary>
        public IReadOnlyDictionary<string, IToolAction> GetTools()
            => _tools;

        // =====================================================================
        // Storage / Memory
        // =====================================================================

        /// <summary>
        /// Returns the configured storage implementation.
        /// Mirrors Mastra.getStorage() from packages/core/src/mastra/index.ts
        /// </summary>
        public IMastraStorage GetStorage() => _storage;

        /// <summary>
        /// Returns a memory instance by key.
        /// Mirrors Mastra.getMemory() from packages/core/src/mastra/index.ts
        /// </summary>
        public MastraMemory GetMemory(string key = null)
        {
            if (key == null)
            {
                foreach (var mem in _memory.Values)
                    return mem;
                return null;
            }

            if (!_memory.ContainsKey(key))
                return null;

            return _memory[key];
        }

        // =====================================================================
        // Logger
        // =====================================================================

        /// <summary>
        /// Returns the configured logger.
        /// Mirrors Mastra.getLogger() from packages/core/src/mastra/index.ts
        /// </summary>
        public IMastraLogger GetLogger() => _logger;

        // =====================================================================
        // PubSub / Events
        // =====================================================================

        /// <summary>
        /// Returns the pub/sub instance.
        /// Mirrors Mastra.getPubSub() from packages/core/src/mastra/index.ts
        /// </summary>
        public IPubSub GetPubSub() => _pubSub;

        /// <summary>
        /// Publishes an event to all registered subscribers.
        /// </summary>
        public Task PublishEventAsync(string topic, MastraEvent evt)
        {
            return _pubSub.PublishAsync(topic, evt);
        }

        // =====================================================================
        // ID generation
        // =====================================================================

        /// <summary>
        /// Generates a unique ID using the configured generator (defaults to Guid.NewGuid).
        /// Mirrors Mastra.getIdGenerator() from packages/core/src/mastra/index.ts
        /// </summary>
        public string GenerateId() => _idGenerator();

        // =====================================================================
        // Private helpers
        // =====================================================================

        private static MastraError CreateUndefinedError(string type, string key)
        {
            var typeName = type;
            var errorId = $"MASTRA_ADD_{type.ToUpper()}_UNDEFINED";
            return new MastraError(new ErrorDefinition
            {
                Id = errorId,
                Domain = ErrorDomain.MASTRA,
                Category = ErrorCategory.USER,
                Text = $"Cannot add {typeName}: {typeName} is null or empty. " +
                       "This may occur if configuration was improperly constructed.",
                Details = key != null
                    ? new Dictionary<string, object> { { "key", key } }
                    : null
            });
        }
    }
}
