// elbruno.Doc2Code — real-time log hub for the agent pipeline.
// Clients join a group keyed by their run identifier so that
// broadcast messages are scoped to the active generation.
//
// Server → Client contract:
//   "ReceiveLog"            — normal log entries (AgentLogEntry)
//   "ReceiveStreamingChunk" — streaming partial updates (AgentLogEntry with IsStreamingChunk=true)
//   "PipelineStatusChanged" — pipeline status updates (PipelineStatus)
//   "PipelineCompleted"     — run completion signal (string runId)
namespace elbruno.Doc2Code.ApiService.Hubs;

using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

public sealed class AgentLogHub(ILogger<AgentLogHub> logger) : Hub
{
    public async Task JoinRunGroup(string runId)
    {
        logger.LogInformation("Conn {C} watching run {R}", Context.ConnectionId, runId);
        await Groups.AddToGroupAsync(Context.ConnectionId, runId);
        await Clients.Caller.SendAsync("Acknowledged", runId);
    }

    public async Task LeaveRunGroup(string runId)
    {
        logger.LogInformation("Conn {C} stopped watching {R}", Context.ConnectionId, runId);
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, runId);
    }
}
