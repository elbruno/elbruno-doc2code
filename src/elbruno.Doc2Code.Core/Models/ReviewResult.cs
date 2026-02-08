// elbruno.Doc2Code — output of the Reviewer agent with quality scoring.
namespace elbruno.Doc2Code.Core.Models;

/// <summary>Quality assessment produced by the code-review agent.</summary>
public sealed class ReviewResult
{
    public int ScoreOutOf100 { get; set; }
    public List<CodeIssue> DetectedIssues { get; init; } = [];
    public List<string> Recommendations { get; init; } = [];
    public string Verdict { get; set; } = "";

    /// <summary>The pipeline re-invokes the Developer when the score is below this bar.</summary>
    public const int AcceptanceBar = 70;
    public bool Acceptable => ScoreOutOf100 >= AcceptanceBar;
}

/// <summary>A single issue found during code review.</summary>
public sealed class CodeIssue
{
    public required string Priority { get; init; }
    public required string Detail { get; init; }
    public string? AffectedFile { get; init; }
    public string? FixHint { get; init; }
}
