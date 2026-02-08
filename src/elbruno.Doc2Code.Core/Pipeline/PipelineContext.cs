// elbruno.Doc2Code — mutable context bag threaded through the agent pipeline.
namespace elbruno.Doc2Code.Core.Pipeline;

using elbruno.Doc2Code.Core.Models;

/// <summary>
/// Carries intermediate results between pipeline stages so each agent
/// can read its predecessor's output and write its own.
/// </summary>
public sealed class PipelineContext
{
    public string RunId { get; init; } = Guid.NewGuid().ToString("N")[..12];

    // Stage outputs — populated progressively
    public RequirementsDocument? Spec { get; set; }
    public AnalysisResult? Analysis { get; set; }
    public ArchitectureBlueprint? Blueprint { get; set; }
    public GeneratedSolution? Code { get; set; }
    public ReviewResult? Review { get; set; }
    public TestSuite? Tests { get; set; }
    public DocumentationBundle? Docs { get; set; }

    // Reviewer loop tracking
    public int ReviewAttempts { get; set; }
    public const int ReviewRetryLimit = 2;
}
