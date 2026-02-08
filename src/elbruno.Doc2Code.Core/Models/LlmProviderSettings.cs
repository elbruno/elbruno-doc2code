// elbruno.Doc2Code — LLM provider enum and provider-specific settings models.
namespace elbruno.Doc2Code.Core.Models;

/// <summary>Supported LLM provider kinds.</summary>
public enum LlmProviderKind
{
    LocalOllama,
    LocalFoundryLocal,
    FoundryOpenAI,
    GitHubCopilot
}

/// <summary>Settings for GitHub Copilot SDK (via Microsoft Agent Framework bridge).</summary>
public sealed class CopilotSettings
{
    public string Model { get; set; } = "gpt-4.1";
    public string? GitHubToken { get; set; }
    public string? CliPath { get; set; }
}

/// <summary>Settings for a local Ollama instance.</summary>
public sealed class OllamaSettings
{
    public string Endpoint { get; set; } = "http://localhost:11434";
    public string Model { get; set; } = "ministral-3";
}

/// <summary>Settings for FoundryLocal (local model via OpenAI-compatible endpoint).</summary>
public sealed class FoundryLocalSettings
{
    public string ModelAlias { get; set; } = "phi-3.5-mini";
    public string Endpoint { get; set; } = "http://localhost:5272/v1";
}

/// <summary>Settings for Azure AI Inference (cloud-hosted model).</summary>
public sealed class AzureAIInferenceSettings
{
    public string Endpoint { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
}
