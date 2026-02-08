# elbruno.Doc2Code — Technical Architecture

## Overview

elbruno.Doc2Code is a multi-agent system that transforms requirements documents
into complete .NET 10 solutions. The architecture follows a sequential pipeline
pattern where each agent processes the output of its predecessor.

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
(`IAgent<TIn,TOut>`, `IGenerationPipeline`, `IDocumentIngester`, `IArchiveBuilder`,
`ISettingsStore`) and the models that flow between agents. Also contains
`AgentInstructions` (default system prompts) and DTOs including
`TestConnectionRequest` / `TestConnectionResult` for provider connectivity testing.

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
Six LLM agents plus an orchestrator:
- **AnalystAgent** — extracts entities, actors, business rules, state machines
- **ArchitectAgent** — designs the target .NET solution structure
- **DeveloperAgent** — generates all source files
- **ReviewerAgent** — scores quality (0–100); can trigger a Developer retry
- **TestingAgent** — generates xUnit tests
- **DocumentationAgent** — generates README and technical documentation

### elbruno.Doc2Code.DocumentProcessing
Parses text/Markdown documents into `RequirementsDocument` using regex-based
heading detection to segment sections.

### elbruno.Doc2Code.SettingsService
Dedicated microservice for configuration persistence:
- `GET /api/settings` — returns full `ConfigurationDto`
- `PUT /api/settings` — replaces the full configuration
- `PATCH /api/settings/tools` — toggles individual tool enabled/disabled states
- `GET /api/settings/models/suggested` — returns model recommendations
- Stores data in a local `settings.json` file with thread-safe access
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
Blazor frontend with Bootstrap 5 (dark terminal theme). Features a real-time
pipeline viewer connected via SignalR, an auto-scrolling console log panel,
and a terminal/hacker aesthetic UI. Includes a Settings page for configuration,
tool toggles, and **Test Connection** buttons for all four LLM providers.

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

```
Document → Ingester → Analyst → Architect → Developer → Reviewer
                                               ↑           │
                                               └── loop ───┘ (if score < 70, max 2 retries)
                                                   → Testing → Documentation → ZIP
```

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
