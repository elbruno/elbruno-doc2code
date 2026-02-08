// elbruno.Doc2Code — DAG-based pipeline runner with parallel execution support.
namespace elbruno.Doc2Code.Agents;

using System.Text.Json;
using elbruno.Doc2Code.Core.Abstractions;
using elbruno.Doc2Code.Core.Models;
using elbruno.Doc2Code.Core.Pipeline;
using elbruno.Doc2Code.ServiceDefaults;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

/// <summary>
/// Runs a dynamic pipeline defined by <see cref="PipelineDefinition"/>.
/// Executes steps level-by-level (parallel within each level) following the DAG topology.
/// </summary>
public sealed class DynamicPipelineRunner : IGenerationPipeline
{
    private readonly IAgentFactory _agentFactory;
    private readonly ISettingsStore _settingsStore;
    private readonly ILogger<DynamicPipelineRunner> _logger;
    private readonly IChatClient _chatClient;
    private readonly TopologicalSorter _sorter;
    private readonly PipelineValidator _validator;

    public DynamicPipelineRunner(
        IAgentFactory agentFactory,
        ISettingsStore settingsStore,
        ILogger<DynamicPipelineRunner> logger,
        IChatClient chatClient,
        TopologicalSorter sorter,
        PipelineValidator validator)
    {
        _agentFactory = agentFactory;
        _settingsStore = settingsStore;
        _logger = logger;
        _chatClient = chatClient;
        _sorter = sorter;
        _validator = validator;
    }

    public async Task<PipelineStatus> RunPipelineAsync(
        RequirementsDocument spec,
        string runId,
        IProgress<AgentLogEntry> log,
        IProgress<PipelineStatus> statusReporter,
        CancellationToken ct = default)
    {
        var status = new PipelineStatus { RunId = runId };

        using var pipelineSpan = Doc2CodeObservability.StartAgentSpan("Pipeline", "execution");

        try
        {
            // Refresh the chat client
            if (_chatClient is IRefreshableChatClient refreshable)
                await refreshable.RefreshAsync(ct);

            var config = await _settingsStore.GetAsync(ct);

            // Find active pipeline
            var pipeline = config.Pipelines.FirstOrDefault(p => p.IsActive)
                ?? PipelineTemplates.CreateDefault();

            pipelineSpan?.SetTag("pipeline.definition.id", pipeline.Id);
            pipelineSpan?.SetTag("pipeline.definition.name", pipeline.Name);
            pipelineSpan?.SetTag("pipeline.definition.version", pipeline.Version);

            // Resolve agent definitions
            var agentDefs = config.AgentDefinitions.Count > 0
                ? config.AgentDefinitions
                : BuiltInAgentDefinitions.Create();

            var agentLookup = agentDefs.ToDictionary(a => a.AgentKey, StringComparer.OrdinalIgnoreCase);

            // Runtime guard: check all referenced agents exist
            foreach (var step in pipeline.Steps)
            {
                if (!agentLookup.ContainsKey(step.AgentKey))
                {
                    var msg = $"Agent '{step.AgentKey}' is referenced in pipeline step '{step.StepId}' but does not exist in the agent repository.";
                    _logger.LogError(msg);
                    status.Stage = WorkflowStage.Faulted;
                    status.FailureReason = msg;
                    status.FinishedAtUtc = DateTime.UtcNow;
                    statusReporter.Report(status);
                    Notify(log, runId, msg, LogSeverity.Failure);
                    return status;
                }
            }

            // Validate pipeline
            var validation = _validator.Validate(pipeline, agentDefs);
            if (!validation.IsValid)
            {
                var msg = $"Pipeline validation failed: {string.Join("; ", validation.Errors)}";
                _logger.LogError(msg);
                status.Stage = WorkflowStage.Faulted;
                status.FailureReason = msg;
                status.FinishedAtUtc = DateTime.UtcNow;
                statusReporter.Report(status);
                Notify(log, runId, msg, LogSeverity.Failure);
                return status;
            }

            // Compute execution levels
            var levels = _sorter.Sort(pipeline);
            var totalSteps = pipeline.Steps.Count;
            status.TotalSteps = totalSteps;

            // Seed the data bag
            var bag = new PipelineDataBag { OriginalSpec = spec };
            bag.Set("spec", spec.ExtractedText);

            Notify(log, runId, $"Pipeline '{pipeline.Name}' started — {totalSteps} steps in {levels.Count} levels");

            var stepIndex = 0;
            for (var levelIdx = 0; levelIdx < levels.Count; levelIdx++)
            {
                var level = levels[levelIdx];
                var peerNames = level.Select(s => s.AgentKey).ToList();

                if (level.Count > 1)
                    Notify(log, runId, $"Level {levelIdx + 1}: running {string.Join(" ∥ ", peerNames)} in parallel");

                var tasks = new List<Task>();

                foreach (var step in level)
                {
                    var currentStepIndex = stepIndex++;
                    var agent = _agentFactory.Create(agentLookup[step.AgentKey]);

                    // Map workflow stage from agent key
                    status.Stage = MapStage(step.AgentKey);
                    status.ActiveAgent = step.AgentKey;
                    status.StepIndex = currentStepIndex;
                    status.StepAgentKey = step.AgentKey;
                    status.ParallelPeers = peerNames;
                    status.CompletionPercent = (int)((double)currentStepIndex / totalSteps * 100);

                    pipelineSpan?.SetTag("pipeline.step.index", currentStepIndex);
                    pipelineSpan?.SetTag("pipeline.level", levelIdx);
                    pipelineSpan?.SetTag("pipeline.total_steps", totalSteps);

                    statusReporter.Report(status);

                    tasks.Add(ExecuteStepWithRetry(agent, step, bag, log, runId, ct));
                }

                await Task.WhenAll(tasks);
            }

            // Extract outputs from the bag
            if (bag.TryGet<GeneratedSolution>("code", out var codeOutput))
                status.CodeOutput = codeOutput;
            if (bag.TryGet<TestSuite>("tests", out var testOutput))
                status.TestOutput = testOutput;
            if (bag.TryGet<DocumentationBundle>("docs", out var docsOutput))
                status.DocsOutput = docsOutput;

            status.Stage = WorkflowStage.Done;
            status.CompletionPercent = 100;
            status.FinishedAtUtc = DateTime.UtcNow;
            statusReporter.Report(status);
            Notify(log, runId, "Pipeline completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Pipeline {RunId} faulted", runId);
            var failureReason = status.ActiveAgent is not null
                ? $"[{status.ActiveAgent}] {ex.Message}"
                : ex.Message;
            status.Stage = WorkflowStage.Faulted;
            status.FailureReason = failureReason;
            status.FinishedAtUtc = DateTime.UtcNow;
            statusReporter.Report(status);
            Notify(log, runId, $"Pipeline faulted: {failureReason}", LogSeverity.Failure);
        }

        return status;
    }

    private async Task ExecuteStepWithRetry(
        IDynamicAgent agent, PipelineStepDefinition step, PipelineDataBag bag,
        IProgress<AgentLogEntry> log, string runId, CancellationToken ct)
    {
        var retry = step.RetryPolicy;
        var maxAttempts = (retry?.MaxRetries ?? 0) + 1;

        for (var attempt = 0; attempt < maxAttempts; attempt++)
        {
            if (attempt > 0)
                Notify(log, runId, $"Retrying {agent.DisplayName} (attempt {attempt + 1}/{maxAttempts})");

            using var span = Doc2CodeObservability.StartAgentSpan(agent.DisplayName, "run");
            await agent.RunAsync(bag, log, ct);

            // Check quality gate
            if (retry is not null && !string.IsNullOrWhiteSpace(retry.QualityGateField))
            {
                if (bag.ContainsKey(agent.AgentKey == "Reviewer" ? "review" : step.AgentKey.ToLowerInvariant()))
                {
                    try
                    {
                        var outputKey = bag.ContainsKey("review") ? "review" : step.AgentKey.ToLowerInvariant();
                        var raw = bag.GetRaw(outputKey);
                        if (raw.TryGetProperty(retry.QualityGateField, out var gateValue) ||
                            raw.TryGetProperty(ToCamelCase(retry.QualityGateField), out gateValue))
                        {
                            var score = gateValue.GetInt32();
                            Notify(log, runId, $"Quality gate: {retry.QualityGateField} = {score} (threshold: {retry.AcceptanceThreshold})");
                            if (score >= retry.AcceptanceThreshold)
                                return; // Passed
                            if (attempt == maxAttempts - 1)
                                Notify(log, runId, $"Quality gate not met after {maxAttempts} attempts — proceeding anyway", LogSeverity.Caution);
                        }
                    }
                    catch
                    {
                        // If we can't read the gate, just continue
                    }
                }
            }
            else
            {
                return; // No retry policy — done
            }
        }
    }

    private static string ToCamelCase(string s) =>
        string.IsNullOrEmpty(s) ? s : char.ToLowerInvariant(s[0]) + s[1..];

    private static WorkflowStage MapStage(string agentKey) => agentKey switch
    {
        "Analyst" => WorkflowStage.Analyzing,
        "Architect" => WorkflowStage.Designing,
        "Developer" => WorkflowStage.Coding,
        "Reviewer" => WorkflowStage.Reviewing,
        "Testing" => WorkflowStage.GeneratingTests,
        "Documentation" => WorkflowStage.GeneratingDocs,
        _ => WorkflowStage.Custom
    };

    private static void Notify(
        IProgress<AgentLogEntry> log, string runId, string text,
        LogSeverity sev = LogSeverity.Informational)
    {
        log.Report(new AgentLogEntry
        {
            SourceAgent = "Orchestrator",
            Text = text,
            Severity = sev,
            RunId = runId
        });
    }
}
