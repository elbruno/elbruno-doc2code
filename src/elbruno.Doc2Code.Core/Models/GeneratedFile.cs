// elbruno.Doc2Code — a single file (source, project, config) inside the generated output.
namespace elbruno.Doc2Code.Core.Models;

/// <summary>One generated source artifact with its relative path and textual content.</summary>
public sealed class CodeArtifact
{
    public required string Path { get; init; }
    public string SourceText { get; set; } = "";
    public string Kind { get; set; } = "cs";
}
