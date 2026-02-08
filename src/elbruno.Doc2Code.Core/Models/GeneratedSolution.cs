// elbruno.Doc2Code — container for all code files produced by the Developer agent.
namespace elbruno.Doc2Code.Core.Models;

/// <summary>The complete generated .NET solution.</summary>
public sealed class GeneratedSolution
{
    public required string SolutionName { get; init; }
    public List<CodeArtifact> Artifacts { get; init; } = [];
    public DateTime CreatedAtUtc { get; init; } = DateTime.UtcNow;

    /// <summary>Total number of generated files.</summary>
    public int ArtifactCount => Artifacts.Count;
}
