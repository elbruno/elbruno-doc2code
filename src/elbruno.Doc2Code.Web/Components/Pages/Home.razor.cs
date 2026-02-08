// elbruno.Doc2Code — code-behind for the generation home page; manages SignalR, uploads, and pipeline state.
namespace elbruno.Doc2Code.Web.Components.Pages;

using System.Text;
using elbruno.Doc2Code.Core.Models;
using elbruno.Doc2Code.Web.Services;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;

/// <summary>
/// Code-behind for the Home page — handles file uploads, pipeline orchestration
/// via SignalR, and archive downloads.
/// </summary>
public sealed partial class Home : IAsyncDisposable
{
    [Microsoft.AspNetCore.Components.Inject]
    public GenerationApiClient GenClient { get; set; } = default!;

    [Microsoft.AspNetCore.Components.Inject]
    public AgentLogSignalRService SignalRLink { get; set; } = default!;

    [Microsoft.AspNetCore.Components.Inject]
    public IJSRuntime JsHost { get; set; } = default!;

    private IBrowserFile? _chosenDoc;
    private bool _pipelineActive;
    private string _statusLine = string.Empty;
    private string? _activeRunId;
    private bool _pipelineFinished;
    private List<AgentLogEntry> _consoleLogs = new();
    private PipelineStatus? _latestPipelineState;
    private string _activeAgentName = string.Empty;
    private int _progressPct;
    private string _currentStageName = string.Empty;
    private Dictionary<string, string> _currentStreamingText = new(StringComparer.OrdinalIgnoreCase);

    private const string ConsolePanelId = "d2c-console-output";

    /// <summary>Maps agent display names to their corresponding <see cref="WorkflowStage"/> for ordering.</summary>
    private static readonly Dictionary<string, WorkflowStage> AgentStageMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Analyst"] = WorkflowStage.Analyzing,
        ["Architect"] = WorkflowStage.Designing,
        ["Developer"] = WorkflowStage.Coding,
        ["Reviewer"] = WorkflowStage.Reviewing,
        ["Testing"] = WorkflowStage.GeneratingTests,
        ["Documentation"] = WorkflowStage.GeneratingDocs,
    };

    private void OnDocumentChosen(InputFileChangeEventArgs args)
    {
        _chosenDoc = args.File;
        _pipelineFinished = false;
        _pipelineActive = false;
        _statusLine = string.Empty;
        _activeRunId = null;
        _consoleLogs.Clear();
        _latestPipelineState = null;
        _activeAgentName = string.Empty;
        _progressPct = 0;
        _currentStageName = string.Empty;
        _currentStreamingText.Clear();
    }

    private async Task LaunchGeneration()
    {
        if (_chosenDoc is null) return;

        _pipelineActive = true;
        _pipelineFinished = false;
        _statusLine = "Uploading document\u2026";
        StateHasChanged();

        try
        {
            await using var docStream = _chosenDoc.OpenReadStream(maxAllowedSize: 10 * 1024 * 1024);
            _activeRunId = await GenClient.SubmitDocumentAsync(docStream, _chosenDoc.Name);
            _statusLine = $"Run {_activeRunId} started. Waiting for agents\u2026";
            StateHasChanged();

            await SignalRLink.SubscribeToRunAsync(_activeRunId);
        }
        catch (Exception exc)
        {
            _statusLine = $"Error: {exc.Message}";
            _pipelineActive = false;
            StateHasChanged();
        }
    }

    protected override async Task OnInitializedAsync()
    {
        SignalRLink.LogArrived += OnLogArrived;
        SignalRLink.PipelineProgressChanged += OnPipelineProgress;
        SignalRLink.RunFinished += OnRunFinished;
        SignalRLink.StreamingChunkArrived += OnStreamingChunkArrived;

        await Task.CompletedTask;
    }

    private void OnLogArrived(AgentLogEntry entry)
    {
        InvokeAsync(async () =>
        {
            _consoleLogs.Add(entry);
            StateHasChanged();
            try
            {
                await JsHost.InvokeVoidAsync("doc2codeScrollToEnd", ConsolePanelId);
            }
            catch
            {
                // JS interop may fail during prerender — safe to ignore.
            }
        });
    }

    private void OnStreamingChunkArrived(AgentLogEntry entry)
    {
        InvokeAsync(async () =>
        {
            _currentStreamingText[entry.SourceAgent] = entry.Text;
            StateHasChanged();
            try
            {
                await JsHost.InvokeVoidAsync("doc2codeScrollToEnd", ConsolePanelId);
            }
            catch
            {
                // JS interop may fail during prerender — safe to ignore.
            }
        });
    }

    private void OnPipelineProgress(PipelineStatus status)
    {
        InvokeAsync(async () =>
        {
            _latestPipelineState = status;
            _activeAgentName = status.ActiveAgent;
            _progressPct = status.CompletionPercent;
            _currentStageName = status.Stage.ToString();
            StateHasChanged();
            try
            {
                await JsHost.InvokeVoidAsync("doc2codeScrollToEnd", ConsolePanelId);
            }
            catch
            {
                // JS interop may fail during prerender — safe to ignore.
            }
        });
    }

    private void OnRunFinished(string runId)
    {
        InvokeAsync(() =>
        {
            _pipelineFinished = true;
            _pipelineActive = false;
            _currentStreamingText.Clear();
            StateHasChanged();
        });
    }

    private async Task DownloadArchive()
    {
        if (_activeRunId is null) return;

        try
        {
            var archiveBytes = await GenClient.DownloadArchiveAsync(_activeRunId);
            var encodedPayload = Convert.ToBase64String(archiveBytes);
            await JsHost.InvokeVoidAsync("doc2codeSaveFile", $"doc2code-{_activeRunId}.zip", encodedPayload);
        }
        catch (Exception exc)
        {
            _statusLine = $"Download failed: {exc.Message}";
            StateHasChanged();
        }
    }

    private void FlushConsole()
    {
        _consoleLogs.Clear();
    }

    private async Task ExportLogs(bool includePrompts)
    {
        var sb = new StringBuilder();
        foreach (var entry in _consoleLogs)
        {
            sb.AppendLine($"[{entry.OccurredAt:HH:mm:ss}] [{entry.SourceAgent}] {entry.Text}");
            if (includePrompts
                && entry.Severity == LogSeverity.Prompt
                && !string.IsNullOrEmpty(entry.ExpandableContent))
            {
                sb.AppendLine("--- Prompt Content ---");
                sb.AppendLine(entry.ExpandableContent);
                sb.AppendLine("--- End Prompt ---");
            }
        }

        try
        {
            var filename = includePrompts ? "doc2code-full-log.txt" : "doc2code-log.txt";
            await JsHost.InvokeVoidAsync("doc2codeSaveText", filename, sb.ToString());
        }
        catch
        {
            // JS interop may fail during prerender — safe to ignore.
        }
    }

    /// <summary>
    /// Returns a CSS class indicating the visual state of a pipeline node.
    /// </summary>
    private string NodeCssState(string agentKey)
    {
        if (_latestPipelineState is null)
            return "d2c-node-idle";

        if (_latestPipelineState.Stage == WorkflowStage.Faulted
            && string.Equals(_activeAgentName, agentKey, StringComparison.OrdinalIgnoreCase))
            return "d2c-node-error";

        if (string.Equals(_activeAgentName, agentKey, StringComparison.OrdinalIgnoreCase))
            return "d2c-node-active";

        // Determine if the agent's stage has already been passed.
        if (AgentStageMap.TryGetValue(agentKey, out var agentStage)
            && AgentStageMap.TryGetValue(_activeAgentName, out var currentStage))
        {
            if (agentStage < currentStage)
                return "d2c-node-done";
        }

        // If pipeline is done, all nodes are done.
        if (_latestPipelineState.Stage == WorkflowStage.Done)
            return "d2c-node-done";

        return "d2c-node-idle";
    }

    /// <summary>
    /// Returns a CSS class indicating the visual state of a pipeline connector arrow.
    /// </summary>
    private string ConnectorCssState(int index)
    {
        if (_latestPipelineState is null)
            return "";

        var agentKeys = new[] { "Analyst", "Architect", "Developer", "Reviewer", "Testing", "Documentation" };
        if (index < 0 || index >= agentKeys.Length - 1)
            return "";

        // The connector before agent at index+1 is active when that agent is active or done
        var nextAgentKey = agentKeys[index + 1];
        var nextState = NodeCssState(nextAgentKey);
        if (nextState is "d2c-node-active" or "d2c-node-done")
            return "d2c-connector-active";

        return "";
    }

    public async ValueTask DisposeAsync()
    {
        SignalRLink.LogArrived -= OnLogArrived;
        SignalRLink.PipelineProgressChanged -= OnPipelineProgress;
        SignalRLink.RunFinished -= OnRunFinished;
        SignalRLink.StreamingChunkArrived -= OnStreamingChunkArrived;

        if (_activeRunId is not null)
        {
            try
            {
                await SignalRLink.UnsubscribeFromRunAsync(_activeRunId);
            }
            catch
            {
                // Connection may already be closed — ignore.
            }
        }
    }
}
