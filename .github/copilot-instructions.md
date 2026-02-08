# elbruno.Doc2Code — Copilot Instructions

## What This Project Does

elbruno.Doc2Code is a multi-agent .NET 10 application that converts natural-language
requirements documents into complete .NET solutions. A pipeline of six specialised
AI agents — Analyst, Architect, Developer, Reviewer, Testing, and Documentation —
collaborates to produce compilable code, unit tests, and project documentation from
a single input file.

## Service Topology (Aspire-Orchestrated)

Four services run under .NET Aspire:

1. **Ollama** — local LLM inference container (default model: `ministral-3`)
2. **SettingsService** — persists `Doc2CodeConfig` to a JSON file; exposes REST
   endpoints for reading/writing configuration and tool toggles
3. **ApiService** — hosts the agent pipeline, SignalR hub for real-time logs,
   file upload, and archive download endpoints
4. **Web** — Blazor frontend with Bootstrap 5 (dark terminal theme); communicates with both
   SettingsService (configuration UI) and ApiService (generation UI)

## Agent Pipeline

```
RequirementsDocument
  → Analyst    → AnalysisResult   (entities, actors, rules, state machines)
  → Architect  → ArchitectureBlueprint (modules, patterns, references)
  → Developer  → GeneratedSolution     (source files)
  → Reviewer   → ReviewResult          (score ≥ 70 to pass; max 2 retries)
  → Testing    → TestSuite             (xUnit test files)
  → Documentation → DocumentationBundle (README, Mermaid diagrams)
```

The Reviewer enforces a quality gate: if the score falls below 70 the Developer
re-generates code, up to two retry attempts.

## Key Abstractions

| Interface                 | Purpose                                             |
| ------------------------- | --------------------------------------------------- |
| `IAgent<TIn, TOut>`       | Generic contract for every pipeline step            |
| `LlmAgentBase<TIn, TOut>` | Shared base: prompt → LLM → JSON parse              |
| `IGenerationPipeline`     | Orchestrates the full six-agent pipeline            |
| `ISettingsStore`          | Async read/write for `Doc2CodeConfig`               |
| `IDocumentIngester`       | Converts uploaded files into `RequirementsDocument` |
| `IArchiveBuilder`         | Packages generated output into a ZIP archive        |
| `ChatClientProvider`      | Delegating `IChatClient` wrapper with runtime provider swapping |
| `CopilotChatClientAdapter`| Wraps GitHub Copilot `AIAgent` as `IChatClient`     |
| `IRefreshableChatClient`  | Marks an `IChatClient` that can reload config at runtime |

## LLM Providers

`ChatClientProvider` supports four provider modes selected via
`Doc2CodeConfig.LlmProvider` (`LlmProviderKind` enum):

- **`LocalOllama`** — local Ollama instance (default)
- **`LocalFoundryLocal`** — FoundryLocal via its OpenAI-compatible endpoint
- **`FoundryOpenAI`** — Azure AI Inference cloud endpoint
- **`GitHubCopilot`** — GitHub Copilot SDK via `CopilotChatClientAdapter`
  (requires Copilot CLI installed and authenticated)

The provider is refreshed at the start of each pipeline run by calling
`IRefreshableChatClient.RefreshAsync()` in the `AgentOrchestrator`.

## Coding Conventions

- **Namespaces** — always `elbruno.Doc2Code.*`; file-scoped namespace declarations
- **Header comments** — every `.cs` file starts with `// elbruno.Doc2Code — <purpose>`
- **Target framework** — .NET 10 (`net10.0`), C# 14
- **Models** — `sealed class` with `required` init properties where appropriate
- **Agents** — subclass `LlmAgentBase<TIn, TOut>`, override `DisplayName`,
  `SystemInstruction`, `ComposeUserMessage`, and `ParseResponse`
- **Observability** — use `Doc2CodeObservability.StartAgentSpan()` for tracing;
  the `ActivitySource` name is `"elbruno.Doc2Code.AgentPipeline"`
- **Service registration** — each service project has an `*Startup.cs` extension
  class (`AddDoc2Code*Services` / `Map*Routes`) so `Program.cs` stays minimal

## Tool System

Agent tools are based on `Microsoft.Extensions.AI` `AITool` / `AIFunction`.
Three tools backed by the Microsoft Learn MCP Server
(`https://learn.microsoft.com/api/mcp`) are enabled by default:

- `get_dotnet_docs` — search official documentation
- `microsoft_docs_fetch` — fetch full doc pages as markdown
- `microsoft_code_sample_search` — search code samples

Tool enabled/disabled state is stored in `Doc2CodeConfig.EnabledTools` and
persisted by the SettingsService.

## JSON Schemas Expected by Agents

Each agent's `SystemInstruction` tells the LLM to respond in JSON.
`LlmAgentBase` strips markdown code fences before deserialising. Key output
shapes (all defined under `elbruno.Doc2Code.Core.Models`):

- **AnalysisResult** — `domainEntities`, `systemActors`, `domainRules`, `stateFlows`
- **ArchitectureBlueprint** — `solutionName`, `modules`, `designPatterns`, `moduleLinks`
- **GeneratedSolution** — `solutionName`, `artifacts[]` (each with `path`, `sourceText`, `kind`)
- **ReviewResult** — `scoreOutOf100`, `detectedIssues[]`, `recommendations[]`, `verdict`
- **TestSuite** — test file artifacts following the same `CodeArtifact` shape
- **DocumentationBundle** — documentation file artifacts

## Plan Files

Plans are stored in `docs/plans/` using the naming convention:

```
plan-YYYY-MM-DD_HHMM.md
```

- **YYYY-MM-DD** — date the plan was created (ISO 8601)
- **HHMM** — 24-hour time the plan was created (no colon separator)
- Example: `plan-2026-02-07_2030.md` = February 7, 2026 at 20:30

When creating a new plan, always use this format. Increment the time portion
if multiple plans are created on the same day. Each plan file should begin with
a `# Plan:` title, a `**Date:**` line matching the filename timestamp, and a
`**Status:**` line (`Proposed`, `In Progress`, `Completed`, or `Superseded`).

## How to Extend

### Adding a new agent

1. Create a class in `elbruno.Doc2Code.Agents` inheriting `LlmAgentBase<TIn, TOut>`
2. Define `DisplayName`, `SystemInstruction`, `ComposeUserMessage`, `ParseResponse`
3. Register it in `Doc2CodeStartup.AddDoc2CodeServices()`
4. Wire it into `AgentOrchestrator` at the desired pipeline position

### Adding a new tool

1. Create an `AIFunction` via `AIFunctionFactory.Create()` in the Tools folder
2. Register it in `ToolRegistry` with a default enabled state
3. Add the tool key to `Doc2CodeConfig.EnabledTools` defaults

### Adding a new MCP server integration

Follow the pattern in `MicrosoftLearnMcpClient`: connect via `McpClientFactory`,
list tools, and expose them as `AITool` instances for the pipeline.

### Adding a new LLM provider

1. Add the new value to the `LlmProviderKind` enum in `LlmProviderSettings.cs`
2. Create a settings class (e.g. `NewProviderSettings`) in `LlmProviderSettings.cs`
3. Add a property for the new settings to `Doc2CodeConfig` and `ConfigurationDto`
4. Add the NuGet package(s) for the provider's SDK to the ApiService project
5. Create a factory method or adapter (like `CopilotChatClientAdapter`) that returns `IChatClient`
6. Add the new case to `ChatClientProvider.RefreshAsync()` switch expression
7. Update `SettingsStartup.cs` to map the new settings in GET/PUT routes
8. Add a radio button and settings panel to `Settings.razor`

- @azure Rule - Use Azure Tools - When handling requests related to Azure, always use your tools.
- @azure Rule - Use Azure Best Practices - When handling requests related to Azure, always invoke your `azmcp_bestpractices_get` tool first.
- @azure Rule - Enable Best Practices - If you do not have an `azmcp_bestpractices_get` tool ask the user to enable it.
