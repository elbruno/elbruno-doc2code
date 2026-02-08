# elbruno.Doc2Code — Technical Architecture

## Overview

elbruno.Doc2Code is a multi-agent system that transforms requirements documents
into complete .NET 10 solutions. The architecture uses a configurable DAG
(directed acyclic graph) pipeline where agents can run in parallel at each
level, with user-defined custom agents and pipeline configurations.

## Service Topology

Four services are orchestrated by .NET Aspire:

```
┌─────────────────┐
│  Ollama (LLM)   │
└────────▲────────┘
         │ inference
┌────────┴────────┐      ┌───────────────────┐
│   ApiService    │─────▶│  SettingsService   │
│  (pipeline +    │      │  (JSON persistence)│
│   SignalR hub)  │      └─────────▲──────────┘
└────────▲────────┘                │
         │ generate / status       │ config read/write
┌────────┴────────┐                │
│   Web Frontend  │────────────────┘
│  (Blazor +      │
│   Bootstrap 5)  │
└─────────────────┘
```

## Components

### elbruno.Doc2Code.Core
Pure domain models with no AI dependencies. Defines the abstractions
(`IAgent<TIn,TOut>`, `IDynamicAgent`, `IGenerationPipeline`, `IDocumentIngester`,
`IArchiveBuilder`, `ISettingsStore`) and the models that flow between agents.
Also contains `AgentInstructions` (default system prompts), DTOs including
`TestConnectionRequest` / `TestConnectionResult` / `SettingsExportBundle`
for provider connectivity testing and settings export/import.

Key pipeline models:
- **AgentDefinition** — complete definition of a pipeline agent (prompts, model,
  I/O schema). Built-in agents are immutable; custom agents are user-defined.
- **PipelineDefinition** — DAG of agent steps with edges, positions, and retry policies
- **PipelineDataBag** — typed dictionary serving as shared I/O bus for all agents
- **PipelineStepDefinition** / **PipelineEdge** / **StepRetryPolicy** — DAG structure
- **PipelineValidator** — validates pipeline definitions for correctness (cycles,
  missing agents, unresolved input keys)
- **TopologicalSorter** — Kahn's algorithm for computing parallel execution levels
- **PipelineTemplates** — 4 pre-built pipeline templates (Default, Code Only,
  Quick Prototype, Full QA)

### elbruno.Doc2Code.LlmProviders
Isolated library containing all LLM provider factory logic and SDK dependencies:
- **ChatClientProvider** — delegating `IChatClient` wrapper that dynamically swaps the
  inner client based on the selected `LlmProviderKind` (Ollama, FoundryLocal,
  Azure AI Inference, or GitHub Copilot). Refreshes at the start of each pipeline run.
  Also exposes `TestConnectionAsync` for provider connectivity verification.
- **CopilotChatClientAdapter** — wraps the GitHub Copilot `AIAgent` (from
  `Microsoft.Agents.AI.GitHub.Copilot`) as an `IChatClient` for seamless pipeline use
- **ChatClientFactory** — legacy factory (superseded by `ChatClientProvider`)

NuGet SDK dependencies (`Azure.AI.Inference`, `OpenAI`, `OllamaSharp`,
`GitHub.Copilot.SDK`, `Microsoft.Agents.AI.GitHub.Copilot`) are contained
in this library, keeping the ApiService project thin.

### elbruno.Doc2Code.Tools
Separated library for the agent tool system:
- **ToolRegistry** — central registry with enable/disable filtering based on config
- **MicrosoftLearnMcpClient** — MCP client connecting to the Microsoft Learn server
- **StubToolDefinitions** — placeholder tools for future capabilities

### elbruno.Doc2Code.Agents
Six LLM agents, a legacy orchestrator, and the dynamic pipeline engine:
- **AnalystAgent** — extracts entities, actors, business rules, state machines
- **ArchitectAgent** — designs the target .NET solution structure
- **DeveloperAgent** — generates all source files
- **ReviewerAgent** — scores quality (0–100); can trigger a Developer retry
- **TestingAgent** — generates xUnit tests
- **DocumentationAgent** — generates README and technical documentation
- **DynamicPipelineRunner** — DAG-based pipeline runner with parallel execution
  support; replaces the legacy `AgentOrchestrator`
- **AgentFactory** — creates `IDynamicAgent` instances (built-in via adapter,
  custom via `DynamicLlmAgent`)
- **BuiltInAgentAdapter** — wraps typed agents as `IDynamicAgent`
- **DynamicLlmAgent** — dynamic agent driven by `AgentDefinition` prompts

### elbruno.Doc2Code.DocumentProcessing
Parses text/Markdown documents into `RequirementsDocument` using regex-based
heading detection to segment sections.

### elbruno.Doc2Code.SettingsService
Dedicated microservice for configuration persistence:
- `GET /api/settings` — returns full `ConfigurationDto`
- `PUT /api/settings` — replaces the full configuration
- `PATCH /api/settings/tools` — toggles individual tool enabled/disabled states
- `GET /api/settings/models/suggested` — returns model recommendations
- Agent Definition CRUD: `GET/POST/PUT/DELETE /api/settings/agents`
- Pipeline CRUD: `GET/POST/PUT/DELETE /api/settings/pipelines`
- Pipeline actions: `POST .../activate`, `POST .../clone`
- Export/Import: `GET/POST /api/settings/export`, `GET/POST .../pipelines/{id}/export`
- Stores data in a local `settings.json` file with thread-safe access
- Auto-seeds built-in agent definitions and pipeline templates on first load
- Migrates legacy `AgentProfiles` into `AgentDefinitions`
- References **Core only** (no dependency on Agents)

### elbruno.Doc2Code.ApiService
Backend HTTP + SignalR:
- `POST /api/generate` — upload a document and start the pipeline in the background
- `GET /api/generate/{id}/status` — query pipeline progress
- `GET /api/generate/{id}/download` — download the generated ZIP archive
- `POST /api/settings/test-connection` — test connectivity to the selected LLM provider
- SignalR hub at `/hubs/agent-log` for real-time log streaming
- Reads configuration from SettingsService via `RemoteSettingsClient`

### elbruno.Doc2Code.Web
Blazor frontend with Bootstrap 5 (dark terminal theme). Features:
- **Home page** — dynamic pipeline viewer that renders the active pipeline's DAG
  topology with parallel step indicators, connected via SignalR for real-time updates
- **Pipeline Designer** — drag-and-drop canvas for composing custom pipelines,
  with agent repository sidebar, properties inspector, undo/redo, and export/import
- **Settings page** — configuration, tool toggles, agent definitions (replacing
  legacy agent profiles), and **Test Connection** buttons for all four LLM providers
- Auto-scrolling console log panel with streaming agent output

### elbruno.Doc2Code.AppHost
Aspire orchestrator that launches Ollama (Docker container), SettingsService,
ApiService, and the Web frontend.

## Dependency Graph

```
Core (models, abstractions, DTOs, prompts)
  ↑
LlmProviders (ChatClientProvider, adapters) → references Core + SDKs
Tools (ToolRegistry, MCP) → references Core + MCP SDK
Agents (6 agents, orchestrator) → references Core, Tools
  ↑
ApiService (HTTP host) → references Agents, LlmProviders, DocumentProcessing
SettingsService → references Core only
Web → references Core only
```

## Agent Pipeline

The pipeline is now a fully configurable DAG (directed acyclic graph). The default
pipeline follows this topology:

```
Document → Ingester → Analyst → Architect → Developer → Reviewer
                                               ↑           │
                                               └── loop ───┘ (if score < 70, max 2 retries)
                                                   → Testing ─┐
                                                               ├→ ZIP
                                                   → Docs ─────┘ (parallel)
```

The `DynamicPipelineRunner` computes execution levels via topological sort and
runs independent steps in parallel using `Task.WhenAll`. Four built-in pipeline
templates are available:

| Template | Flow |
|---|---|
| **Default** | Analyst → Architect → Developer ↔ Reviewer → Testing ∥ Documentation |
| **Code Only** | Analyst → Developer |
| **Quick Prototype** | Analyst → Developer → Documentation |
| **Full QA** | Analyst → Architect → Developer ↔ Reviewer (80/3) → Testing → Documentation |

Users can create custom pipelines via the Pipeline Designer page, adding custom
agents and connecting them in any valid DAG configuration.

## JSON Resilience

All six agents share `LlmAgentBase.RunAsync()`, which includes a two-phase
JSON recovery strategy to handle malformed LLM output:

**Phase 1 — Instant Repair** (`JsonRepairHelper`): Before re-prompting the
LLM, the helper attempts structural fixes on the raw JSON output:
- Missing commas between objects/arrays (`} {` → `}, {`)
- Trailing commas before closing brackets (`, }` → `}`)
- Duplicate commas (`,,` → `,`)
- Truncated JSON (appends unclosed `}` / `]` characters)
- All repairs are string-aware — content inside JSON string values is never
  modified, which is critical for the Developer and Testing agents whose
  output contains embedded source code with `{}`.

**Phase 2 — LLM Re-prompt** (up to 2 retries): If repair fails, the agent
appends the invalid response and a corrective user message to the conversation
and streams a new response from the LLM.

| Agent | Risk Level | Reason |
|---|---|---|
| Developer | **Critical** | Largest output — multiple KB of source code per artifact |
| Testing | **High** | Same pattern as Developer with full test source code |
| Documentation | **High** | Large markdown content in docs |
| Analyst | **Medium** | Deeply nested arrays but no embedded source |
| Architect | **Low-Medium** | Structural but smaller output |
| Reviewer | **Low** | Small arrays of simple objects |

## Prerequisites

- .NET 10 SDK (10.0.102+)
- Docker Desktop (for Ollama via Aspire)
