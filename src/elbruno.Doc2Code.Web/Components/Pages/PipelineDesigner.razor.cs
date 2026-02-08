// elbruno.Doc2Code — code-behind for the Pipeline Designer page.
namespace elbruno.Doc2Code.Web.Components.Pages;

using System.Text.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using elbruno.Doc2Code.Core.Models;
using elbruno.Doc2Code.Core.Pipeline;
using elbruno.Doc2Code.Web.Services;

/// <summary>
/// Manages the Pipeline Designer page state: agent repository, canvas rendering,
/// properties inspector, undo/redo, and pipeline CRUD operations.
/// </summary>
public sealed partial class PipelineDesigner : ComponentBase, IAsyncDisposable
{
    [Inject] private SettingsApiClient SettingsClient { get; set; } = default!;
    [Inject] private IJSRuntime Js { get; set; } = default!;

    // ── Data ────────────────────────────────────────────────────────
    private List<AgentDefinition> _agents = [];
    private List<PipelineDefinition> _pipelines = [];
    private PipelineDefinition? _activePipeline;
    private PipelineStepDefinition? _selectedStep;
    private AgentDefinition? _selectedStepAgent;
    private readonly List<PipelineDefinition> _templates = PipelineTemplates.CreateAll();

    // ── UI state ────────────────────────────────────────────────────
    private bool _loadFailed;
    private string _statusMessage = "";
    private bool _statusIsError;
    private bool _showAgentModal;
    private AgentDefinition? _editingAgent;
    private bool _showImportInput;
    private string _importJson = "";
    // ── Connect mode ────────────────────────────────────────────────────
    private bool _isConnectMode;
    private string? _connectSourceStepId;
    // ── Validation ──────────────────────────────────────────────────
    private PipelineValidationResult? _validationResult;
    private readonly PipelineValidator _validator = new();

    // ── Undo / Redo ─────────────────────────────────────────────────
    private readonly List<string> _undoStack = [];
    private readonly List<string> _redoStack = [];
    private const int MaxUndoDepth = 40;

    // ── JS interop ──────────────────────────────────────────────────
    private DotNetObjectReference<PipelineDesigner>? _selfRef;
    private const string CanvasContainerId = "pd-canvas-container";

    private static readonly JsonSerializerOptions _jsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    // ── Lifecycle ───────────────────────────────────────────────────

    protected override async Task OnInitializedAsync()
    {
        try
        {
            _agents = await SettingsClient.GetAgentDefinitionsAsync();
            _pipelines = await SettingsClient.GetPipelinesAsync();
            _activePipeline = _pipelines.FirstOrDefault(p => p.IsActive)
                              ?? _pipelines.FirstOrDefault();
        }
        catch
        {
            _loadFailed = true;
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _selfRef = DotNetObjectReference.Create(this);
            await RenderCanvasAsync();
        }
    }

    public async ValueTask DisposeAsync()
    {
        _selfRef?.Dispose();
        await ValueTask.CompletedTask;
    }

    // ── Canvas rendering ────────────────────────────────────────────

    private async Task RenderCanvasAsync()
    {
        if (_activePipeline is null || _selfRef is null) return;
        var payload = BuildCanvasPayload(_activePipeline);
        await Js.InvokeVoidAsync("pipelineCanvas.initCanvas", _selfRef, CanvasContainerId, payload);
    }

    private object BuildCanvasPayload(PipelineDefinition pipeline)
    {
        var agentNames = _agents.ToDictionary(a => a.AgentKey, a => a.DisplayName);
        return new
        {
            steps = pipeline.Steps,
            edges = pipeline.Edges,
            _agentNames = agentNames
        };
    }

    // ── JS callbacks ────────────────────────────────────────────────

    [JSInvokable("OnNodeMovedJs")]
    public void HandleNodeMoved(string stepId, double x, double y)
    {
        if (_activePipeline is null) return;
        var step = _activePipeline.Steps.Find(s => s.StepId == stepId);
        if (step is null) return;
        step.PositionX = x;
        step.PositionY = y;
        InvokeAsync(StateHasChanged);
    }

    [JSInvokable("OnEdgeCreatedJs")]
    public void HandleEdgeCreated(string sourceId, string targetId)
    {
        if (_activePipeline is null) return;
        var alreadyExists = _activePipeline.Edges.Any(e =>
            e.SourceStepId == sourceId && e.TargetStepId == targetId);
        if (alreadyExists) return;

        PushUndo();
        _activePipeline.Edges.Add(new PipelineEdge
        {
            SourceStepId = sourceId,
            TargetStepId = targetId,
            OutputKeyMapping = ""
        });
        InvokeAsync(async () => { StateHasChanged(); await RenderCanvasAsync(); });
    }

    [JSInvokable("OnNodeSelectedJs")]
    public void HandleNodeSelected(string stepId)
    {
        if (_activePipeline is null) return;

        // If in connect mode, handle connect-mode click
        if (_isConnectMode)
        {
            HandleConnectModeClick(stepId);
            return;
        }

        _selectedStep = _activePipeline.Steps.Find(s => s.StepId == stepId);
        _selectedStepAgent = _selectedStep is not null
            ? _agents.Find(a => a.AgentKey == _selectedStep.AgentKey)
            : null;
        InvokeAsync(StateHasChanged);
    }

    [JSInvokable("OnNodeDeletedJs")]
    public void HandleNodeDeleted(string stepId)
    {
        if (_activePipeline is null) return;
        var step = _activePipeline.Steps.Find(s => s.StepId == stepId);
        if (step is not null && step.IsBookend)
        {
            ShowStatus("Bookend steps cannot be removed.", isError: true);
            InvokeAsync(async () => { StateHasChanged(); await RenderCanvasAsync(); });
            return;
        }
        PushUndo();
        _activePipeline.Steps.RemoveAll(s => s.StepId == stepId);
        _activePipeline.Edges.RemoveAll(e => e.SourceStepId == stepId || e.TargetStepId == stepId);
        if (_selectedStep?.StepId == stepId) { _selectedStep = null; _selectedStepAgent = null; }
        InvokeAsync(async () => { StateHasChanged(); await RenderCanvasAsync(); });
    }

    // ── Pipeline selection ──────────────────────────────────────────

    private async Task SelectPipelineAsync(PipelineDefinition pipeline)
    {
        _activePipeline = pipeline;
        _selectedStep = null;
        _selectedStepAgent = null;
        _validationResult = null;
        _undoStack.Clear();
        _redoStack.Clear();
        await RenderCanvasAsync();
    }

    // ── Toolbar actions ─────────────────────────────────────────────

    private async Task SavePipelineAsync()
    {
        if (_activePipeline is null) return;
        try
        {
            await SettingsClient.UpdatePipelineAsync(_activePipeline.Id, _activePipeline);
            ShowStatus("Pipeline saved.");
        }
        catch (Exception ex) { ShowStatus("Save failed: " + ex.Message, isError: true); }
    }

    private async Task ClonePipelineAsync()
    {
        if (_activePipeline is null) return;
        try
        {
            var cloned = await SettingsClient.ClonePipelineAsync(_activePipeline.Id);
            if (cloned is not null)
            {
                _pipelines.Add(cloned);
                await SelectPipelineAsync(cloned);
                ShowStatus("Pipeline cloned.");
            }
        }
        catch (Exception ex) { ShowStatus("Clone failed: " + ex.Message, isError: true); }
    }

    private async Task DeletePipelineAsync()
    {
        if (_activePipeline is null || _activePipeline.IsDefault) return;
        try
        {
            await SettingsClient.DeletePipelineAsync(_activePipeline.Id);
            _pipelines.Remove(_activePipeline);
            _activePipeline = _pipelines.FirstOrDefault();
            _selectedStep = null;
            _selectedStepAgent = null;
            await RenderCanvasAsync();
            ShowStatus("Pipeline deleted.");
        }
        catch (Exception ex) { ShowStatus("Delete failed: " + ex.Message, isError: true); }
    }

    private async Task ActivatePipelineAsync()
    {
        if (_activePipeline is null) return;
        try
        {
            await SettingsClient.ActivatePipelineAsync(_activePipeline.Id);
            foreach (var p in _pipelines) p.IsActive = false;
            _activePipeline.IsActive = true;
            ShowStatus("Pipeline activated.");
        }
        catch (Exception ex) { ShowStatus("Activate failed: " + ex.Message, isError: true); }
    }

    private void ValidatePipeline()
    {
        if (_activePipeline is null) return;
        _validationResult = _validator.Validate(_activePipeline, _agents);
        ShowStatus(_validationResult.IsValid ? "Validation passed." : "Validation found issues.");
    }

    // ── Export / Import ─────────────────────────────────────────────

    private async Task ExportPipelineJsonAsync()
    {
        if (_activePipeline is null) return;
        try
        {
            var json = JsonSerializer.Serialize(_activePipeline, _jsonOpts);
            await Js.InvokeVoidAsync("doc2codeSaveText",
                (_activePipeline.Name ?? "pipeline") + ".json", json);
            ShowStatus("Pipeline exported.");
        }
        catch (Exception ex) { ShowStatus("Export failed: " + ex.Message, isError: true); }
    }

    private async Task ImportPipelineJsonAsync()
    {
        if (string.IsNullOrWhiteSpace(_importJson)) return;
        try
        {
            var imported = JsonSerializer.Deserialize<PipelineDefinition>(_importJson, _jsonOpts);
            if (imported is null) { ShowStatus("Invalid JSON.", isError: true); return; }
            imported.Id = Guid.NewGuid().ToString();
            var created = await SettingsClient.CreatePipelineAsync(imported);
            if (created is not null)
            {
                _pipelines.Add(created);
                await SelectPipelineAsync(created);
            }
            _importJson = "";
            _showImportInput = false;
            ShowStatus("Pipeline imported.");
        }
        catch (Exception ex) { ShowStatus("Import failed: " + ex.Message, isError: true); }
    }

    // ── Drag agent to canvas ────────────────────────────────────────

    private async Task AddAgentToCanvasAsync(AgentDefinition agent)
    {
        if (_activePipeline is null) return;
        PushUndo();

        var maxX = _activePipeline.Steps.Count > 0
            ? _activePipeline.Steps.Max(s => s.PositionX) + 220
            : 40;
        var newStep = new PipelineStepDefinition
        {
            StepId = Guid.NewGuid().ToString(),
            AgentKey = agent.AgentKey,
            PositionX = maxX,
            PositionY = 80
        };
        _activePipeline.Steps.Add(newStep);

        // Auto-connect to last step if there is one
        if (_activePipeline.Steps.Count > 1)
        {
            var previousStep = _activePipeline.Steps[^2];
            var sourceAgent = _agents.Find(a => a.AgentKey == previousStep.AgentKey);
            _activePipeline.Edges.Add(new PipelineEdge
            {
                SourceStepId = previousStep.StepId,
                TargetStepId = newStep.StepId,
                OutputKeyMapping = sourceAgent?.OutputKey ?? ""
            });
        }

        await RenderCanvasAsync();
    }

    // ── Step property changes ───────────────────────────────────────

    private async Task RemoveSelectedStepAsync()
    {
        if (_activePipeline is null || _selectedStep is null) return;
        if (_selectedStep.IsBookend)
        {
            ShowStatus("Bookend steps cannot be removed.", isError: true);
            return;
        }
        PushUndo();
        var stepId = _selectedStep.StepId;
        _activePipeline.Steps.RemoveAll(s => s.StepId == stepId);
        _activePipeline.Edges.RemoveAll(e => e.SourceStepId == stepId || e.TargetStepId == stepId);
        _selectedStep = null;
        _selectedStepAgent = null;
        await RenderCanvasAsync();
    }

    private void ToggleRetryPolicy()
    {
        if (_selectedStep is null) return;
        PushUndo();
        _selectedStep.RetryPolicy = _selectedStep.RetryPolicy is null
            ? new StepRetryPolicy { MaxRetries = 2, AcceptanceThreshold = 70, QualityGateField = "scoreOutOf100" }
            : null;
    }

    // ── Edge helpers (properties panel) ─────────────────────────────

    private List<PipelineEdge> GetInboundEdges()
    {
        if (_activePipeline is null || _selectedStep is null) return [];
        return _activePipeline.Edges.Where(e => e.TargetStepId == _selectedStep.StepId).ToList();
    }

    private List<PipelineEdge> GetOutboundEdges()
    {
        if (_activePipeline is null || _selectedStep is null) return [];
        return _activePipeline.Edges.Where(e => e.SourceStepId == _selectedStep.StepId).ToList();
    }

    private string ResolveStepLabel(string stepId)
    {
        if (_activePipeline is null) return stepId;
        var step = _activePipeline.Steps.Find(s => s.StepId == stepId);
        if (step is null) return stepId;
        var agent = _agents.Find(a => a.AgentKey == step.AgentKey);
        return agent?.DisplayName ?? step.AgentKey;
    }

    // ── Agent modal ─────────────────────────────────────────────────

    private void OpenNewAgentModal()
    {
        _editingAgent = new AgentDefinition
        {
            AgentKey = "",
            DisplayName = "",
            IsBuiltIn = false,
            Temperature = 0.7,
            ModelId = "ministral-3"
        };
        _showAgentModal = true;
    }

    private void OpenEditAgentModal(AgentDefinition agent)
    {
        _editingAgent = agent;
        _showAgentModal = true;
    }

    private async Task HandleAgentSavedAsync(AgentDefinition saved)
    {
        var existing = _agents.FindIndex(a => a.AgentKey == saved.AgentKey);
        if (existing >= 0)
        {
            _agents[existing] = saved;
            await SettingsClient.UpdateAgentAsync(saved.AgentKey, saved);
        }
        else
        {
            _agents.Add(saved);
            await SettingsClient.CreateAgentAsync(saved);
        }
        _showAgentModal = false;
        _editingAgent = null;
    }

    private void CloseAgentModal()
    {
        _showAgentModal = false;
        _editingAgent = null;
    }

    // ── Undo / Redo ─────────────────────────────────────────────────

    private void PushUndo()
    {
        if (_activePipeline is null) return;
        var snapshot = JsonSerializer.Serialize(_activePipeline, _jsonOpts);
        _undoStack.Add(snapshot);
        if (_undoStack.Count > MaxUndoDepth) _undoStack.RemoveAt(0);
        _redoStack.Clear();
    }

    private async Task UndoAsync()
    {
        if (_undoStack.Count == 0 || _activePipeline is null) return;
        var currentSnapshot = JsonSerializer.Serialize(_activePipeline, _jsonOpts);
        _redoStack.Add(currentSnapshot);

        var previous = _undoStack[^1];
        _undoStack.RemoveAt(_undoStack.Count - 1);
        var restored = JsonSerializer.Deserialize<PipelineDefinition>(previous, _jsonOpts);
        if (restored is not null)
        {
            // Preserve identity
            restored.Id = _activePipeline.Id;
            var idx = _pipelines.IndexOf(_activePipeline);
            _activePipeline = restored;
            if (idx >= 0) _pipelines[idx] = restored;
            _selectedStep = null;
            _selectedStepAgent = null;
            await RenderCanvasAsync();
        }
    }

    private async Task RedoAsync()
    {
        if (_redoStack.Count == 0 || _activePipeline is null) return;
        var currentSnapshot = JsonSerializer.Serialize(_activePipeline, _jsonOpts);
        _undoStack.Add(currentSnapshot);

        var next = _redoStack[^1];
        _redoStack.RemoveAt(_redoStack.Count - 1);
        var restored = JsonSerializer.Deserialize<PipelineDefinition>(next, _jsonOpts);
        if (restored is not null)
        {
            restored.Id = _activePipeline.Id;
            var idx = _pipelines.IndexOf(_activePipeline);
            _activePipeline = restored;
            if (idx >= 0) _pipelines[idx] = restored;
            _selectedStep = null;
            _selectedStepAgent = null;
            await RenderCanvasAsync();
        }
    }

    // ── Auto-layout ─────────────────────────────────────────────────

    private async Task AutoLayoutAsync()
    {
        await Js.InvokeVoidAsync("pipelineCanvas.autoLayout");
        ShowStatus("Layout applied.");
    }

    // ── Template loading ────────────────────────────────────────────

    private async Task LoadTemplateAsync(PipelineDefinition template)
    {
        try
        {
            // Deep-clone the template so each use is independent
            var json = JsonSerializer.Serialize(template, _jsonOpts);
            var cloned = JsonSerializer.Deserialize<PipelineDefinition>(json, _jsonOpts);
            if (cloned is null) { ShowStatus("Failed to load template.", isError: true); return; }

            cloned.Id = Guid.NewGuid().ToString();
            cloned.Name = template.Name + " (copy)";
            cloned.IsDefault = false;
            cloned.IsActive = false;

            var created = await SettingsClient.CreatePipelineAsync(cloned);
            if (created is not null)
            {
                _pipelines.Add(created);
                await SelectPipelineAsync(created);
                ShowStatus($"Pipeline created from template: {template.Name}");
            }
            else
            {
                ShowStatus("Backend did not return the created pipeline.", isError: true);
            }
        }
        catch (Exception ex)
        {
            ShowStatus($"Template load failed: {ex.Message}", isError: true);
        }
    }

    // ── Helpers ──────────────────────────────────────────────────────

    private void ShowStatus(string message, bool isError = false)
    {
        _statusMessage = message;
        _statusIsError = isError;
    }

    // ── Connect mode ───────────────────────────────────────────────────

    private void ToggleConnectMode()
    {
        _isConnectMode = !_isConnectMode;
        _connectSourceStepId = null;
        if (_isConnectMode)
            ShowStatus("Connect mode ON — click a source step, then a target step to create an edge.");
        else
            ShowStatus("Connect mode cancelled.");
    }

    private void StartConnectFromSelected()
    {
        if (_selectedStep is null) return;
        _isConnectMode = true;
        _connectSourceStepId = _selectedStep.StepId;
        ShowStatus($"Source set: {ResolveStepLabel(_selectedStep.StepId)} — now click the target step.");
    }

    private void HandleConnectModeClick(string stepId)
    {
        if (_activePipeline is null) return;

        if (_connectSourceStepId is null)
        {
            // First click: select source
            _connectSourceStepId = stepId;
            ShowStatus($"Source: {ResolveStepLabel(stepId)} — now click the target step.");
            InvokeAsync(StateHasChanged);
            return;
        }

        // Second click: create edge
        if (_connectSourceStepId == stepId)
        {
            ShowStatus("Cannot connect a step to itself. Click a different step.", isError: true);
            InvokeAsync(StateHasChanged);
            return;
        }

        var alreadyExists = _activePipeline.Edges.Any(e =>
            e.SourceStepId == _connectSourceStepId && e.TargetStepId == stepId);
        if (alreadyExists)
        {
            ShowStatus("This edge already exists.", isError: true);
            _connectSourceStepId = null;
            InvokeAsync(StateHasChanged);
            return;
        }

        // Resolve the source agent's output key for default mapping
        var sourceStep = _activePipeline.Steps.Find(s => s.StepId == _connectSourceStepId);
        var sourceAgent = sourceStep is not null ? _agents.Find(a => a.AgentKey == sourceStep.AgentKey) : null;

        PushUndo();
        _activePipeline.Edges.Add(new PipelineEdge
        {
            SourceStepId = _connectSourceStepId,
            TargetStepId = stepId,
            OutputKeyMapping = sourceAgent?.OutputKey ?? ""
        });

        ShowStatus($"Edge created: {ResolveStepLabel(_connectSourceStepId)} \u2192 {ResolveStepLabel(stepId)}");
        _connectSourceStepId = null;
        _isConnectMode = false;

        InvokeAsync(async () => { StateHasChanged(); await RenderCanvasAsync(); });
    }

    // ── Edge management ───────────────────────────────────────────────

    private async Task RemoveEdgeAsync(PipelineEdge edge)
    {
        if (_activePipeline is null) return;
        PushUndo();
        _activePipeline.Edges.Remove(edge);
        ShowStatus($"Edge removed: {ResolveStepLabel(edge.SourceStepId)} \u2192 {ResolveStepLabel(edge.TargetStepId)}");
        await RenderCanvasAsync();
    }

    private IEnumerable<AgentDefinition> BuiltInAgents => _agents.Where(a => a.IsBuiltIn);
    private IEnumerable<AgentDefinition> CustomAgents => _agents.Where(a => !a.IsBuiltIn);
    private bool HasUndoHistory => _undoStack.Count > 0;
    private bool HasRedoHistory => _redoStack.Count > 0;
}
