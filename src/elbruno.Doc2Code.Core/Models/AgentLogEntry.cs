// elbruno.Doc2Code — structured log entry emitted by agents during pipeline execution.
namespace elbruno.Doc2Code.Core.Models;

/// <summary>A single log message produced by an agent.</summary>
public sealed class AgentLogEntry
{
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
    public required string SourceAgent { get; init; }
    public required string Text { get; init; }
    public LogSeverity Severity { get; init; } = LogSeverity.Informational;
    public string? RunId { get; init; }

    /// <summary>Full prompt text or streaming chunk accumulation, rendered as collapsible in the UI.</summary>
    public string? ExpandableContent { get; init; }

    /// <summary>Distinguishes streaming partial updates from normal log entries.</summary>
    public bool IsStreamingChunk { get; init; }
}

/// <summary>Severity levels for agent log messages.</summary>
public enum LogSeverity
{
    Trace,
    Informational,
    Caution,
    Failure,
    /// <summary>Prompt log entries that get the expandable treatment.</summary>
    Prompt
}
