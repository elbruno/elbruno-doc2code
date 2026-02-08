// elbruno.Doc2Code — describes a recommended local LLM with hardware requirements.
namespace elbruno.Doc2Code.Core.Models;

/// <summary>A suggested local model with its VRAM requirements and pull command.</summary>
public sealed class ModelSuggestion
{
    public required string Name { get; init; }
    public int MinVramGb { get; init; }
    public string Description { get; init; } = "";
    public string OllamaPullCommand { get; init; } = "";
}
