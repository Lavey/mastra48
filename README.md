# mastra48

A port of [mastra-ai/mastra](https://github.com/mastra-ai/mastra) to **.NET 4.8 / C# 7.3**, preserving the original project structure and logic.

---

## Project Structure

```
Aype.AI.sln
└── src/
    ├── Aype.AI.Core/          # Core library (targeting net48, LangVersion 7.3)
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
    ├── Aype.AI.Tests/         # NUnit 3 unit tests (42 tests)
    │   └── MastraTests.cs
    └── Aype.AI.Examples/      # Runnable console examples for every feature area
        └── Program.cs          # Agent, Tools, Workflow, Memory, Events, Logger examples
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
using Aype.AI;
using Aype.AI.Agent;
using Aype.AI.Llm;
using Aype.AI.Logger;
using Aype.AI.Storage;
using Aype.AI.Workflows;
using Aype.AI.Tools;
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
| `MastraError` | `Aype.AI.Error.MastraError` |
| `LogLevel` | `Aype.AI.Logger.LogLevel` |
| `EventEmitterPubSub` | `Aype.AI.Events.EventEmitterPubSub` |
| `InMemoryStorage` | `Aype.AI.Storage.InMemoryStorage` |
| `InMemoryMemory` | `Aype.AI.Memory.InMemoryMemory` |

---

## Examples

The `src/Aype.AI.Examples/` project contains a runnable console application that demonstrates every major feature of the library:

| Example | What it shows |
|---|---|
| **Basic Agent** | Create a `Mastra` instance with an agent, inject a stub language model, call `GenerateAsync` |
| **Tools** | Create typed tools with `ToolFactory.CreateTool<TIn,TOut>`, execute them directly and look them up from `Mastra` |
| **Workflow** | Build a multi-step pipeline with `WorkflowFactory` + `.Then()`, run it via `RunAsync`, inspect per-step results |
| **Memory** | Use `InMemoryMemory` to save/retrieve conversation messages across turns, then delete the thread |
| **Events / PubSub** | Subscribe to topics via `MastraConfig.Events`, publish events with `PublishEventAsync` |
| **Logger** | Compare `ConsoleLogger` (stdout) and `NoopLogger` (silent) |

Run all examples with:

```bash
dotnet run --project src/Aype.AI.Examples/Aype.AI.Examples.csproj
```

---

## Building & Testing

### Windows (Visual Studio / MSBuild)

```bash
# Restore NuGet packages
nuget restore Aype.AI.sln

# Build
msbuild Aype.AI.sln /p:Configuration=Release

# Test (requires NUnit Console Runner or VS Test Explorer)
nunit3-console src\Aype.AI.Tests\bin\Release\net48\Aype.AI.Tests.dll
```

### Linux / macOS (for development / CI verification with net8.0 target)

The code is also verified with `net8.0` target and C# 7.3 language version on Linux:

```bash
# Build
dotnet build src/Aype.AI.Core/Aype.AI.Core.csproj

# Test (42 tests, all passing)
dotnet test src/Aype.AI.Tests/Aype.AI.Tests.csproj

# Run examples
dotnet run --project src/Aype.AI.Examples/Aype.AI.Examples.csproj
```

---

## License

MIT — see the original [mastra-ai/mastra](https://github.com/mastra-ai/mastra) repository.
