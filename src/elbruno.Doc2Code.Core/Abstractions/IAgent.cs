// elbruno.Doc2Code — generic contract for every agent in the pipeline.
namespace elbruno.Doc2Code.Core.Abstractions;

using elbruno.Doc2Code.Core.Models;

/// <summary>
/// A processing step that transforms <typeparamref name="TIn"/> into <typeparamref name="TOut"/>,
/// reporting progress through <see cref="AgentLogEntry"/> messages.
/// </summary>
public interface IAgent<TIn, TOut>
{
    /// <summary>Human-readable label for dashboard / log display.</summary>
    string DisplayName { get; }

    /// <summary>Run this agent's logic.</summary>
    Task<TOut> RunAsync(
        TIn payload,
        IProgress<AgentLogEntry> log,
        CancellationToken ct = default);
}
