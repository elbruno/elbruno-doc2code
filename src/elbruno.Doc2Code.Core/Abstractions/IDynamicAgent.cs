// elbruno.Doc2Code — contract for agents in the dynamic pipeline.
namespace elbruno.Doc2Code.Core.Abstractions;

using elbruno.Doc2Code.Core.Models;

/// <summary>
/// A pipeline agent that reads from and writes to a <see cref="PipelineDataBag"/>.
/// Implementations include built-in agent adapters and user-defined dynamic agents.
/// </summary>
public interface IDynamicAgent
{
    /// <summary>Unique agent key matching the <see cref="AgentDefinition.AgentKey"/>.</summary>
    string AgentKey { get; }

    /// <summary>Human-readable display name.</summary>
    string DisplayName { get; }

    /// <summary>Executes the agent, reading inputs from and writing outputs to the bag.</summary>
    Task<PipelineDataBag> RunAsync(PipelineDataBag input, IProgress<AgentLogEntry> log, CancellationToken ct);
}
