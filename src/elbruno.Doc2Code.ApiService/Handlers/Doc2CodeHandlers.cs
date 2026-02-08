// elbruno.Doc2Code — handler methods for /api/generate and /api/generate/{id}/github.
// These are plain instance methods invoked by the endpoint mapping in Program.cs.
namespace elbruno.Doc2Code.ApiService.Handlers;

using elbruno.Doc2Code.ApiService.Hubs;
using elbruno.Doc2Code.ApiService.Services;
using elbruno.Doc2Code.Core.Abstractions;
using elbruno.Doc2Code.Core.DTOs;
using elbruno.Doc2Code.Core.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;

/// <summary>
/// Contains all HTTP handler logic for the Doc2Code API surface.
/// Injected as a singleton and referenced directly from endpoint mappings.
/// </summary>
public sealed class Doc2CodeHandlers(
    IDocumentIngester ingester,
    IGenerationPipeline pipeline,
    IArchiveBuilder archiver,
    IHubContext<AgentLogHub> hub,
    RunTracker tracker,
    GitHubPublisher publisher)
{
    // ─── POST /api/generate ─────────────────────────────────────────
    public async Task<IResult> StartGeneration(HttpRequest req)
    {
        var form = await req.ReadFormAsync();
        var uploaded = form.Files.FirstOrDefault();
        if (uploaded is null || uploaded.Length == 0)
            return Results.BadRequest(new { detail = "Attach a document file." });

        var runId = RunTracker.NewRunId();
        await using var docStream = uploaded.OpenReadStream();
        var parsedSpec = await ingester.IngestAsync(docStream, uploaded.FileName);

        tracker.Register(new PipelineStatus { RunId = runId, Stage = WorkflowStage.Idle });

        var reporter = new Progress<AgentLogEntry>(async entry =>
        {
            var tagged = new AgentLogEntry
            {
                SourceAgent = entry.SourceAgent,
                Text = entry.Text,
                Severity = entry.Severity,
                RunId = runId,
                ExpandableContent = entry.ExpandableContent,
                IsStreamingChunk = entry.IsStreamingChunk
            };

            if (entry.IsStreamingChunk)
            {
                await hub.Clients.Group(runId).SendAsync("ReceiveStreamingChunk", tagged);
            }
            else
            {
                await hub.Clients.Group(runId).SendAsync("ReceiveLog", tagged);
            }
        });

        var statusReporter = new Progress<PipelineStatus>(async status =>
        {
            tracker.Update(status);
            await hub.Clients.Group(runId).SendAsync("PipelineStatusChanged", status);
        });

        _ = Task.Run(async () =>
        {
            var outcome = await pipeline.RunPipelineAsync(parsedSpec, runId, reporter, statusReporter);
            tracker.Update(outcome);
            await hub.Clients.Group(runId).SendAsync("PipelineStatusChanged", outcome);
            if (outcome.Stage == WorkflowStage.Done)
                await hub.Clients.Group(runId).SendAsync("PipelineCompleted", runId);
        });

        return Results.Accepted(
            $"/api/generate/{runId}/status",
            new StartGenerationResponse { RunId = runId, StatusMessage = "Accepted" });
    }

    // ─── GET /api/generate/{runId}/status ────────────────────────────
    public IResult GetStatus(string runId)
    {
        var snap = tracker.TryGet(runId);
        return snap is not null
            ? Results.Ok(snap)
            : Results.NotFound(new { detail = $"Unknown run '{runId}'" });
    }

    // ─── GET /api/generate/{runId}/download ──────────────────────────
    public async Task<IResult> DownloadArchive(string runId)
    {
        var snap = tracker.TryGet(runId);
        if (snap is null)
            return Results.NotFound(new { detail = $"Unknown run '{runId}'" });
        if (snap.Stage != WorkflowStage.Done || snap.CodeOutput is null)
            return Results.BadRequest(new { detail = "Run has not completed." });

        var zip = await archiver.BuildArchiveAsync(snap.CodeOutput, snap.TestOutput, snap.DocsOutput);
        return Results.File(zip, "application/zip", $"{snap.CodeOutput.SolutionName}.zip");
    }

    // ─── POST /api/generate/{runId}/github ───────────────────────────
    public IResult PublishToGitHub(string runId)
    {
        // Will delegate to publisher once Octokit integration is wired up
        _ = publisher; // suppress unused-parameter warning until then
        return Results.StatusCode(StatusCodes.Status501NotImplemented);
    }
}
