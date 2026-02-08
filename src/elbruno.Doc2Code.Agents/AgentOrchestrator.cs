// elbruno.Doc2Code — orchestrates the six agents in sequence with a review loop.
namespace elbruno.Doc2Code.Agents;

using elbruno.Doc2Code.Core.Abstractions;
using elbruno.Doc2Code.Core.Models;
using elbruno.Doc2Code.Core.Pipeline;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

/// <summary>
/// Runs the complete agent pipeline:
///   Analyst → Architect → Developer → Reviewer (loop if score &lt; 70, max 2) → Testing → Documentation
/// Each step emits <see cref="AgentLogEntry"/> messages through the provided <see cref="IProgress{T}"/>.
/// </summary>
public sealed class AgentOrchestrator : IGenerationPipeline
{
    private readonly AnalystAgent _analyst;
    private readonly ArchitectAgent _architect;
    private readonly DeveloperAgent _developer;
    private readonly ReviewerAgent _reviewer;
    private readonly TestingAgent _tester;
    private readonly DocumentationAgent _documenter;
    private readonly ISettingsStore _settingsStore;
    private readonly ILogger<AgentOrchestrator> _logger;
    private readonly IChatClient _chatClient;

    public AgentOrchestrator(
        AnalystAgent analyst,
        ArchitectAgent architect,
        DeveloperAgent developer,
        ReviewerAgent reviewer,
        TestingAgent tester,
        DocumentationAgent documenter,
        ISettingsStore settingsStore,
        ILogger<AgentOrchestrator> logger,
        IChatClient chatClient)
    {
        _analyst = analyst;
        _architect = architect;
        _developer = developer;
        _reviewer = reviewer;
        _tester = tester;
        _documenter = documenter;
        _settingsStore = settingsStore;
        _logger = logger;
        _chatClient = chatClient;
    }

    /// <inheritdoc />
    public async Task<PipelineStatus> RunPipelineAsync(
        RequirementsDocument spec,
        string runId,
        IProgress<AgentLogEntry> log,
        IProgress<PipelineStatus> statusReporter,
        CancellationToken ct = default)
    {
        var status = new PipelineStatus { RunId = runId };

        try
        {
            // Refresh the chat client to use the latest provider/model settings
            if (_chatClient is IRefreshableChatClient refreshable)
            {
                await refreshable.RefreshAsync(ct);
            }

            // Read current tool toggles from settings
            var config = await _settingsStore.GetAsync(ct);
            var toolToggles = config.EnabledTools;

            // Propagate tool toggles to all agents
            _analyst.EnabledToolToggles = toolToggles;
            _architect.EnabledToolToggles = toolToggles;
            _developer.EnabledToolToggles = toolToggles;
            _reviewer.EnabledToolToggles = toolToggles;
            _tester.EnabledToolToggles = toolToggles;
            _documenter.EnabledToolToggles = toolToggles;
            // 1. Analyse
            status.Stage = WorkflowStage.Analyzing;
            status.ActiveAgent = _analyst.DisplayName;
            status.CompletionPercent = 10;
            statusReporter.Report(status);
            Notify(log, runId, "Pipeline started — running Analyst");
            var analysis = await _analyst.RunAsync(spec, log, ct);

            // 2. Design architecture
            status.Stage = WorkflowStage.Designing;
            status.ActiveAgent = _architect.DisplayName;
            status.CompletionPercent = 25;
            statusReporter.Report(status);
            Notify(log, runId, "Running Architect");
            var blueprint = await _architect.RunAsync(analysis, log, ct);

            // 3. Generate code (with review loop)
            var ctx = new PipelineContext { Spec = spec, Analysis = analysis, Blueprint = blueprint };
            GeneratedSolution code;
            ReviewResult review;

            do
            {
                status.Stage = WorkflowStage.Coding;
                status.ActiveAgent = _developer.DisplayName;
                status.CompletionPercent = 40 + (ctx.ReviewAttempts * 10);
                statusReporter.Report(status);
                Notify(log, runId, ctx.ReviewAttempts > 0
                    ? $"Re-running Developer (attempt {ctx.ReviewAttempts + 1})"
                    : "Running Developer");
                code = await _developer.RunAsync((analysis, blueprint), log, ct);

                status.Stage = WorkflowStage.Reviewing;
                status.ActiveAgent = _reviewer.DisplayName;
                status.CompletionPercent = 55 + (ctx.ReviewAttempts * 10);
                statusReporter.Report(status);
                Notify(log, runId, "Running Reviewer");
                review = await _reviewer.RunAsync((code, analysis, blueprint), log, ct);

                ctx.ReviewAttempts++;
                Notify(log, runId, $"Review score: {review.ScoreOutOf100}/100 (bar: {ReviewResult.AcceptanceBar})");
            }
            while (!review.Acceptable && ctx.ReviewAttempts < PipelineContext.ReviewRetryLimit);

            ctx.Code = code;
            ctx.Review = review;

            // 4. Generate tests
            status.Stage = WorkflowStage.GeneratingTests;
            status.ActiveAgent = _tester.DisplayName;
            status.CompletionPercent = 75;
            statusReporter.Report(status);
            Notify(log, runId, "Running Testing agent");
            var tests = await _tester.RunAsync((code, analysis), log, ct);
            ctx.Tests = tests;

            // 5. Generate documentation
            status.Stage = WorkflowStage.GeneratingDocs;
            status.ActiveAgent = _documenter.DisplayName;
            status.CompletionPercent = 90;
            statusReporter.Report(status);
            Notify(log, runId, "Running Documentation agent");
            var docs = await _documenter.RunAsync((code, blueprint), log, ct);
            ctx.Docs = docs;

            // Done
            status.Stage = WorkflowStage.Done;
            status.CompletionPercent = 100;
            status.FinishedAtUtc = DateTime.UtcNow;
            status.CodeOutput = code;
            status.TestOutput = tests;
            status.DocsOutput = docs;
            statusReporter.Report(status);
            Notify(log, runId, "Pipeline completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Pipeline {RunId} faulted", runId);
            var failureReason = status.ActiveAgent is not null
                ? $"[{status.ActiveAgent}] {ex.Message}"
                : ex.Message;
            if (ex is System.Text.Json.JsonException jsonEx)
            {
                failureReason += $" (JSON path: {jsonEx.Path}, line: {jsonEx.LineNumber})";
            }
            status.Stage = WorkflowStage.Faulted;
            status.FailureReason = failureReason;
            status.FinishedAtUtc = DateTime.UtcNow;
            statusReporter.Report(status);
            Notify(log, runId, $"Pipeline faulted: {failureReason}", LogSeverity.Failure);
        }

        return status;
    }

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
