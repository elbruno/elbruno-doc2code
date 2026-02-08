// elbruno.Doc2Code — DTO for reading / writing application settings from the Settings page.
namespace elbruno.Doc2Code.Core.DTOs;

using elbruno.Doc2Code.Core.Models;

/// <summary>Flat settings object exchanged between the UI and the API.</summary>
public sealed class ConfigurationDto
{
    public LlmProviderKind LlmProvider { get; set; } = LlmProviderKind.LocalOllama;
    public OllamaSettings Ollama { get; set; } = new();
    public FoundryLocalSettings FoundryLocal { get; set; } = new();
    public AzureAIInferenceSettings AzureAIInference { get; set; } = new();
    public CopilotSettings Copilot { get; set; } = new();

    public string LlmEndpoint { get; set; } = "http://localhost:11434";
    public string PreferredModel { get; set; } = "ministral-3";
    public List<AgentProfile> AgentProfiles { get; set; } = [];
    public string? GitHubToken { get; set; }
    public string? GitHubOwner { get; set; }
    public Dictionary<string, bool> EnabledTools { get; set; } = new();
    public List<ModelSuggestion>? SuggestedModels { get; set; }

    /// <summary>Default agent system prompts, keyed by agent name. Populated by the API (read-only on client).</summary>
    public Dictionary<string, AgentDefaultInfo>? AgentDefaults { get; set; }
}

/// <summary>Request body for the test-connection endpoint.</summary>
public sealed class TestConnectionRequest
{
    public LlmProviderKind Provider { get; set; }
    public OllamaSettings Ollama { get; set; } = new();
    public FoundryLocalSettings FoundryLocal { get; set; } = new();
    public AzureAIInferenceSettings AzureAIInference { get; set; } = new();
    public CopilotSettings Copilot { get; set; } = new();
}

/// <summary>Result returned by the test-connection endpoint.</summary>
public sealed class TestConnectionResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
}
