// elbruno.Doc2Code — test artifacts produced by the Testing agent.
namespace elbruno.Doc2Code.Core.Models;

/// <summary>Collection of test files generated for the solution.</summary>
public sealed class TestSuite
{
    public List<CodeArtifact> TestArtifacts { get; init; } = [];
    public string TestModuleName { get; set; } = "";
    public int EstimatedTestCount { get; set; }
    public string Notes { get; set; } = "";
}
