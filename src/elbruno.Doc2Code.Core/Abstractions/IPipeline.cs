// elbruno.Doc2Code — orchestrates the full agent pipeline from document to solution.
namespace elbruno.Doc2Code.Core.Abstractions;

using elbruno.Doc2Code.Core.Models;

/// <summary>Runs the complete multi-agent pipeline for a single generation request.</summary>
public interface IGenerationPipeline
{
    Task<PipelineStatus> RunPipelineAsync(
        RequirementsDocument spec,
        string runId,
        IProgress<AgentLogEntry> log,
        IProgress<PipelineStatus> statusReporter,
        CancellationToken ct = default);
}
