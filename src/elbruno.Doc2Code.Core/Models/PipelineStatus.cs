// elbruno.Doc2Code — tracks the progress of the multi-agent pipeline for a given run.
namespace elbruno.Doc2Code.Core.Models;

/// <summary>Snapshot of the pipeline's current progress.</summary>
public sealed class PipelineStatus
{
    public required string RunId { get; init; }
    public string ActiveAgent { get; set; } = "";
    public WorkflowStage Stage { get; set; } = WorkflowStage.Idle;
    public int CompletionPercent { get; set; }
    public DateTime BeganAtUtc { get; init; } = DateTime.UtcNow;
    public DateTime? FinishedAtUtc { get; set; }
    public string? FailureReason { get; set; }

    // Attached outputs — populated as the pipeline progresses
    public GeneratedSolution? CodeOutput { get; set; }
    public TestSuite? TestOutput { get; set; }
    public DocumentationBundle? DocsOutput { get; set; }
}

/// <summary>High-level stages the pipeline transitions through.</summary>
public enum WorkflowStage
{
    Idle,
    Ingesting,
    Analyzing,
    Designing,
    Coding,
    Reviewing,
    GeneratingTests,
    GeneratingDocs,
    Done,
    Faulted
}
