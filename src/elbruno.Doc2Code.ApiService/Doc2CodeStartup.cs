// elbruno.Doc2Code — collects all DI registrations and route mappings for the API service
// into a single extension class so that Program.cs stays minimal.
namespace elbruno.Doc2Code.ApiService;

using elbruno.Doc2Code.Agents;
using elbruno.Doc2Code.Tools;
using elbruno.Doc2Code.ApiService.Handlers;
using elbruno.Doc2Code.ApiService.Hubs;
using elbruno.Doc2Code.ApiService.Services;
using elbruno.Doc2Code.Core.Abstractions;
using elbruno.Doc2Code.Core.DTOs;
using elbruno.Doc2Code.DocumentProcessing;
using Microsoft.Extensions.AI;
using elbruno.Doc2Code.LlmProviders;

/// <summary>
/// Registers every Doc2Code-specific service (agents, pipeline, handlers)
/// and maps all HTTP + SignalR routes.
/// </summary>
public static class Doc2CodeStartup
{
    /// <summary>Maps the SignalR hub and all HTTP endpoints.</summary>
    public static void MapDoc2CodeRoutes(this WebApplication app)
    {
        app.MapDefaultEndpoints();
        app.MapHub<AgentLogHub>("/hubs/agent-log");

        var api = app.Services.GetRequiredService<Doc2CodeHandlers>();

        app.MapPost("/api/generate", api.StartGeneration).DisableAntiforgery();
        app.MapGet("/api/generate/{runId}/status", (string runId) => api.GetStatus(runId));
        app.MapGet("/api/generate/{runId}/download", (string runId) => api.DownloadArchive(runId));
        app.MapPost("/api/generate/{runId}/github", (string runId) => api.PublishToGitHub(runId));

        app.MapPost("/api/settings/test-connection", async (TestConnectionRequest request, ChatClientProvider provider, CancellationToken ct) =>
        {
            var result = await provider.TestConnectionAsync(request, ct);
            return Results.Ok(result);
        });
    }
}
