# Configuration Reference

## `Doc2CodeConfig` Properties

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `LlmProvider` | `LlmProviderKind` | `LocalOllama` | Active LLM provider mode |
| `Ollama` | `OllamaSettings` | see below | Ollama-specific configuration |
| `FoundryLocal` | `FoundryLocalSettings` | see below | FoundryLocal-specific configuration |
| `AzureAIInference` | `AzureAIInferenceSettings` | see below | Azure AI Inference configuration |
| `Copilot` | `CopilotSettings` | see below | GitHub Copilot SDK configuration |
| `AgentProfiles` | `List<AgentProfile>` | `[]` | Per-agent overrides (model, creativity, instruction) |
| `SourceControl.Token` | `string?` | `null` | GitHub PAT for publishing |
| `SourceControl.OwnerOrOrg` | `string?` | `null` | GitHub target owner/org |
| `EnabledTools` | `Dictionary<string, bool>` | See below | Tool toggle map |
| `SuggestedModels` | `List<ModelSuggestion>` | 4 defaults | Informational model recommendations |

## Provider Modes

The `LlmProvider` property selects which backend is used for all agents.
The `ChatClientProvider` service reads this at the start of each pipeline run
and creates the appropriate `IChatClient`.

### `LocalOllama` (default)

| Setting | Default | Description |
|---------|---------|-------------|
| `Ollama.Endpoint` | `http://localhost:11434` | Ollama server URL |
| `Ollama.Model` | `ministral-3` | Model name passed to Ollama |

### `LocalFoundryLocal`

| Setting | Default | Description |
|---------|---------|-------------|
| `FoundryLocal.ModelAlias` | `phi-3.5-mini` | Model alias used by FoundryLocal |
| `FoundryLocal.Endpoint` | `http://localhost:5272/v1` | FoundryLocal OpenAI-compatible endpoint |

### `FoundryOpenAI` (Azure AI Inference)

| Setting | Default | Description |
|---------|---------|-------------|
| `AzureAIInference.Endpoint` | `""` | Azure AI Inference endpoint URL |
| `AzureAIInference.Model` | `""` | Model deployment name |
| `AzureAIInference.ApiKey` | `""` | Azure API key |

### `GitHubCopilot`

| Setting | Default | Description |
|---------|---------|-------------|
| `Copilot.Model` | `gpt-4.1` | Model to use (e.g. `gpt-4.1`, `claude-sonnet-4.5`) |
| `Copilot.GitHubToken` | `null` | GitHub token (optional if CLI is authenticated) |
| `Copilot.CliPath` | `null` | Path to `copilot` CLI binary (optional if on PATH) |

### Default Tool Toggles

| Key | Default |
|-----|---------|
| `get_dotnet_docs` | `true` |
| `microsoft_docs_fetch` | `true` |
| `microsoft_code_sample_search` | `true` |
| `web_search` | `false` |
| `file_read` | `false` |
| `code_compile_check` | `false` |
| `nuget_search` | `false` |
| `run_unit_tests` | `false` |

## SettingsService REST Endpoints

| Verb | Path | Body | Returns |
|------|------|------|---------|
| `GET` | `/api/settings` | — | `ConfigurationDto` |
| `PUT` | `/api/settings` | `ConfigurationDto` | `{ saved: true }` |
| `PATCH` | `/api/settings/tools` | `Dictionary<string, bool>` | Updated tool map |
| `GET` | `/api/settings/models/suggested` | — | `List<ModelSuggestion>` |
| `GET` | `/health` | — | Health-check status |

## ApiService REST Endpoints

| Verb | Path | Body | Returns |
|------|------|------|---------|
| `POST` | `/api/settings/test-connection` | `TestConnectionRequest` | `TestConnectionResult` |

The **test-connection** endpoint creates a temporary `IChatClient` for the
requested provider, sends a minimal probe message ("Hi" with max 1 token),
and returns `{ success: true/false, message: "..." }`. The request carries
a 15-second timeout.

## Environment Variables

The SettingsService reads `SettingsService:DataDirectory` from configuration
(defaults to `"data"`). This controls where `settings.json` is written.

Ollama connection details can be overridden with `Ollama:Endpoint` and
`Ollama:Model` in the ApiService configuration, but at runtime the values
in `Doc2CodeConfig` (from the SettingsService) take precedence when the
`ChatClientProvider` refreshes the active `IChatClient`.

## Settings UI

The Settings page uses a 4-tab layout:

1. **Agent Mode** — Provider selector radio group + conditional per-provider settings panels.
   Each provider panel includes **[Test Connection]** and **[Help]** buttons.
2. **Agent Tools** — MCP and future tool toggles
3. **Agent Profiles** — Per-agent model, creativity, and instruction overrides
4. **Source Control** — GitHub token and owner/org for publishing
