// elbruno.Doc2Code — global application configuration (Ollama endpoint, model, GitHub creds).
namespace elbruno.Doc2Code.Core.Models;

using System.Text.Json.Serialization;

/// <summary>Top-level configuration persisted by the API service.</summary>
public sealed class Doc2CodeConfig
{
    public LlmProviderKind LlmProvider { get; set; } = LlmProviderKind.LocalOllama;
    public OllamaSettings Ollama { get; set; } = new();
    public FoundryLocalSettings FoundryLocal { get; set; } = new();
    public AzureAIInferenceSettings AzureAIInference { get; set; } = new();
    public CopilotSettings Copilot { get; set; } = new();

    /// <summary>Backward-compatible getter — reads from <see cref="Ollama.Endpoint"/>.</summary>
    [JsonIgnore]
    public string LlmEndpoint
    {
        get => Ollama.Endpoint;
        set => Ollama.Endpoint = value;
    }

    /// <summary>Backward-compatible getter — reads from <see cref="Ollama.Model"/>.</summary>
    [JsonIgnore]
    public string PreferredModel
    {
        get => Ollama.Model;
        set => Ollama.Model = value;
    }

    public List<AgentProfile> AgentProfiles { get; init; } = [];
    public SourceControlConfig SourceControl { get; init; } = new();

    public Dictionary<string, bool> EnabledTools { get; set; } = new()
    {
        ["get_dotnet_docs"] = true,
        ["microsoft_docs_fetch"] = true,
        ["microsoft_code_sample_search"] = true,
        ["web_search"] = false,
        ["file_read"] = false,
        ["code_compile_check"] = false,
        ["nuget_search"] = false,
        ["run_unit_tests"] = false,
    };

    public List<ModelSuggestion> SuggestedModels { get; init; } =
    [
        new() { Name = "ministral-3", MinVramGb = 4, Description = "General-purpose, works on most hardware", OllamaPullCommand = "ollama pull ministral-3" },
        new() { Name = "devstral-small-2", MinVramGb = 8, Description = "Mistral code-optimized model", OllamaPullCommand = "ollama pull devstral-small:24b" },
        new() { Name = "qwen3-coder-next", MinVramGb = 12, Description = "Strong multi-language code generation", OllamaPullCommand = "ollama pull qwen3:32b" },
    ];
}

/// <summary>GitHub publishing credentials.</summary>
public sealed class SourceControlConfig
{
    public string? Token { get; set; }
    public string? OwnerOrOrg { get; set; }
}
