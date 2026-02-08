// elbruno.Doc2Code — documentation artifacts produced by the Documentation agent.
namespace elbruno.Doc2Code.Core.Models;

/// <summary>All generated documentation for the solution.</summary>
public sealed class DocumentationBundle
{
    public CodeArtifact ReadmeFile { get; set; } = new() { Path = "README.md" };
    public CodeArtifact ArchGuide { get; set; } = new() { Path = "docs/architecture.md" };
    public List<CodeArtifact> ExtraDocs { get; init; } = [];
    public string EntityDiagramMermaid { get; set; } = "";
}
