// elbruno.Doc2Code — tracks active and completed pipeline runs in memory.
// A production build would persist this to a database or distributed cache.
namespace elbruno.Doc2Code.ApiService.Services;

using System.Collections.Concurrent;
using elbruno.Doc2Code.Core.Models;

/// <summary>
/// Thread-safe registry of pipeline runs.  Both the HTTP handlers and
/// the background pipeline task read/write through this single service
/// so there is one source of truth.
/// </summary>
public sealed class RunTracker
{
    private readonly ConcurrentDictionary<string, PipelineStatus> _runs = new();

    /// <summary>Generates a short hex identifier based on the current UTC tick count.</summary>
    public static string NewRunId() => DateTime.UtcNow.Ticks.ToString("x")[^10..];

    public void Register(PipelineStatus initial) => _runs[initial.RunId] = initial;

    public void Update(PipelineStatus updated) => _runs[updated.RunId] = updated;

    public PipelineStatus? TryGet(string runId)
        => _runs.TryGetValue(runId, out var s) ? s : null;
}
