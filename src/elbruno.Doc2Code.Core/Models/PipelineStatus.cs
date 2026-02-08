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

    /// <summary>Zero-based index of the current step in the pipeline.</summary>
    public int StepIndex { get; set; }

    /// <summary>Total number of steps in the pipeline.</summary>
    public int TotalSteps { get; set; }

    /// <summary>Agent key of the step currently executing.</summary>
    public string StepAgentKey { get; set; } = "";

    /// <summary>Display names of agents running concurrently in the same level.</summary>
    public List<string> ParallelPeers { get; set; } = [];

    /// <summary>Custom stage name when <see cref="Stage"/> is <see cref="WorkflowStage.Custom"/>.</summary>
    public string? CustomStageName { get; set; }

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
    Faulted,
    Custom
}
