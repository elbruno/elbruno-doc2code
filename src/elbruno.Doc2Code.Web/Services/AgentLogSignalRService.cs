// elbruno.Doc2Code — SignalR client for real-time agent log and pipeline status streaming.
namespace elbruno.Doc2Code.Web.Services;

using elbruno.Doc2Code.Core.Models;
using Microsoft.AspNetCore.SignalR.Client;

/// <summary>
/// Wraps a <see cref="HubConnection"/> to the ApiService agent-log hub,
/// raising CLR events when log entries, status updates, or completion
/// signals arrive from the server.
/// </summary>
public sealed class AgentLogSignalRService : IAsyncDisposable
{
    private readonly HubConnection _hub;

    public event Action<AgentLogEntry>? LogArrived;
    public event Action<PipelineStatus>? PipelineProgressChanged;
    public event Action<string>? RunFinished;
    public event Action<AgentLogEntry>? StreamingChunkArrived;

    public AgentLogSignalRService(IConfiguration appConfig)
    {
        var baseUrl = appConfig["services:apiservice:https:0"]
                      ?? appConfig["services:apiservice:http:0"]
                      ?? "https://localhost:5001";

        _hub = new HubConnectionBuilder()
            .WithUrl($"{baseUrl}/hubs/agent-log")
            .WithAutomaticReconnect()
            .Build();

        _hub.On<AgentLogEntry>("ReceiveLog", entry =>
            LogArrived?.Invoke(entry));

        _hub.On<PipelineStatus>("PipelineStatusChanged", status =>
            PipelineProgressChanged?.Invoke(status));

        _hub.On<string>("PipelineCompleted", runId =>
            RunFinished?.Invoke(runId));

        _hub.On<AgentLogEntry>("ReceiveStreamingChunk", entry =>
            StreamingChunkArrived?.Invoke(entry));
    }

    /// <summary>
    /// Starts the underlying connection when it is not already active.
    /// </summary>
    public async Task EnsureConnectedAsync()
    {
        if (_hub.State == HubConnectionState.Disconnected)
        {
            await _hub.StartAsync();
        }
    }

    /// <summary>
    /// Joins the server-side group for the given run so this client
    /// receives scoped broadcast messages.
    /// </summary>
    public async Task SubscribeToRunAsync(string runId)
    {
        await EnsureConnectedAsync();
        await _hub.InvokeAsync("JoinRunGroup", runId);
    }

    /// <summary>
    /// Leaves the server-side group for the given run, if connected.
    /// </summary>
    public async Task UnsubscribeFromRunAsync(string runId)
    {
        if (_hub.State == HubConnectionState.Connected)
        {
            await _hub.InvokeAsync("LeaveRunGroup", runId);
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await _hub.StopAsync();
        await _hub.DisposeAsync();
    }
}
