# mastra48

A port of [mastra-ai/mastra](https://github.com/mastra-ai/mastra) to **.NET 4.8 / C# 7.3**, preserving the original project structure and logic.

---

## Project Structure

```
Mastra48.sln
└── src/
    ├── Mastra48.Core/          # Core library (targeting net48, LangVersion 7.3)
    │   ├── Mastra.cs           # Central orchestrator (mirrors packages/core/src/mastra/)
    │   ├── Agent/
    │   │   ├── Agent.cs        # AI agent class (mirrors packages/core/src/agent/agent.ts)
    │   │   └── AgentConfig.cs  # Agent configuration and result types
    │   ├── Tools/
    │   │   ├── Tool.cs         # Tool<TInput,TOutput> + ToolFactory (mirrors tools/tool.ts)
    │   │   └── ToolTypes.cs    # IToolAction, ToolExecutionContext, ValidationError
    │   ├── Workflows/
    │   │   ├── Workflow.cs     # Workflow orchestrator (mirrors workflows/workflow.ts)
    │   │   └── Step.cs         # Step + StepFactory (mirrors workflows/step.ts)
    │   ├── Memory/
    │   │   └── Memory.cs       # MastraMemory base + InMemoryMemory
    │   ├── Storage/
    │   │   ├── IMastraStorage.cs   # Storage interface + domain types
    │   │   └── InMemoryStorage.cs  # In-memory implementation
    │   ├── Logger/
    │   │   ├── LogLevel.cs         # LogLevel enum
    │   │   ├── MastraLogger.cs     # IMastraLogger + abstract MastraLogger
    │   │   ├── ConsoleLogger.cs    # Console-based logger
    │   │   └── NoopLogger.cs       # No-operation logger
    │   ├── Error/
    │   │   ├── ErrorDomain.cs      # ErrorDomain enum
    │   │   ├── ErrorCategory.cs    # ErrorCategory enum
    │   │   └── MastraError.cs      # MastraError exception class
    │   ├── Events/
    │   │   └── PubSub.cs           # IPubSub + EventEmitterPubSub
    │   └── Llm/
    │       └── LanguageModel.cs    # IMastraLanguageModel + supporting types
    └── Mastra48.Tests/         # NUnit 3 unit tests (42 tests)
        └── MastraTests.cs
```

---

## Requirements

- **Visual Studio 2017+** or **MSBuild 15+** targeting .NET Framework 4.8
- **.NET Framework 4.8** runtime installed on the target machine
- **NuGet** package restore (Newtonsoft.Json 13.x, NUnit 3.x)

> **Note:** This project targets `net48` and uses the C# 7.3 language version.
> It cannot be built with the `dotnet` CLI alone on Linux/macOS without
> .NET Framework 4.8 reference assemblies (included via the Windows SDK or
> [Microsoft.NETFramework.ReferenceAssemblies](https://www.nuget.org/packages/Microsoft.NETFramework.ReferenceAssemblies)).
> All code is syntactically valid C# 7.3 and has been verified to compile and
> run all tests against the equivalent .NET 8 target on Linux.

---

## Quick Start

```csharp
using Mastra48;
using Mastra48.Agent;
using Mastra48.Llm;
using Mastra48.Logger;
using Mastra48.Storage;
using Mastra48.Workflows;
using Mastra48.Tools;
using System.Collections.Generic;
using System.Threading.Tasks;

// 1. Define a tool
var greetTool = ToolFactory.CreateTool<string, string>(
    id: "greet",
    description: "Greets the user by name",
    execute: async (name, ctx) => await Task.FromResult($"Hello, {name}!"));

// 2. Define a workflow step
var step = StepFactory.CreateStep(
    "process",
    async ctx =>
    {
        var input = ctx.Input?.ToString() ?? "world";
        return (object)$"Processed: {input}";
    });

// 3. Define a workflow
var workflow = WorkflowFactory.CreateWorkflow("demo-workflow")
    .Then(step);

// 4. Create a Mastra instance
var mastra = new Mastra(new MastraConfig
{
    Agents = new Dictionary<string, Agent>
    {
        { "myAgent", new Agent(new AgentConfig
          {
              Id   = "my-agent",
              Name = "My Agent",
              Instructions = "You are a helpful assistant.",
              Model = new ModelConfig { ModelId = "openai/gpt-4o" },
              Tools = new Dictionary<string, IToolAction> { { "greet", greetTool } }
          })
        }
    },
    Workflows = new Dictionary<string, Workflow> { { "demo", workflow } },
    Storage   = new InMemoryStorage(),
    Logger    = new ConsoleLogger("MyApp", LogLevel.INFO)
});

// 5. Run the workflow
var result = await mastra.GetWorkflow("demo").RunAsync(
    new WorkflowRunOptions { Input = "Mastra" });

Console.WriteLine(result.Status);       // Completed
Console.WriteLine(result.FinalOutput);  // Processed: Mastra
```

---

## Key Concepts

| TypeScript (original) | C# port |
|---|---|
| `new Mastra({ ... })` | `new Mastra(new MastraConfig { ... })` |
| `new Agent({ id, name, instructions, model })` | `new Agent(new AgentConfig { ... })` |
| `createTool({ id, description, execute })` | `ToolFactory.CreateTool<TIn,TOut>(id, desc, execute)` |
| `createStep({ id, execute })` | `StepFactory.CreateStep(id, execute)` |
| `createWorkflow(id).then(step1).then(step2)` | `WorkflowFactory.CreateWorkflow(id).Then(step1).Then(step2)` |
| `agent.generate({ prompt })` | `await agent.GenerateAsync(new AgentGenerateOptions { Prompt = ... })` |
| `workflow.execute({ input })` | `await workflow.RunAsync(new WorkflowRunOptions { Input = ... })` |
| `MastraError` | `Mastra48.Error.MastraError` |
| `LogLevel` | `Mastra48.Logger.LogLevel` |
| `EventEmitterPubSub` | `Mastra48.Events.EventEmitterPubSub` |
| `InMemoryStorage` | `Mastra48.Storage.InMemoryStorage` |
| `InMemoryMemory` | `Mastra48.Memory.InMemoryMemory` |

---

## Building & Testing

### Windows (Visual Studio / MSBuild)

```bash
# Restore NuGet packages
nuget restore Mastra48.sln

# Build
msbuild Mastra48.sln /p:Configuration=Release

# Test (requires NUnit Console Runner or VS Test Explorer)
nunit3-console src\Mastra48.Tests\bin\Release\net48\Mastra48.Tests.dll
```

### Linux / macOS (for development / CI verification with net8.0 target)

The code is also verified with `net8.0` target and C# 7.3 language version on Linux:

```bash
# Build
dotnet build src/Mastra48.Core/Mastra48.Core.csproj

# Test (42 tests, all passing)
dotnet test src/Mastra48.Tests/Mastra48.Tests.csproj
```

---

## License

MIT — see the original [mastra-ai/mastra](https://github.com/mastra-ai/mastra) repository.
