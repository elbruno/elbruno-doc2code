// elbruno.Doc2Code — adapts existing built-in agents to the IDynamicAgent interface.
namespace elbruno.Doc2Code.Agents;

using System.Text.Json;
using elbruno.Doc2Code.Core.Abstractions;
using elbruno.Doc2Code.Core.Models;

/// <summary>
/// Wraps a strongly-typed built-in <see cref="LlmAgentBase{TIn,TOut}"/> agent
/// as an <see cref="IDynamicAgent"/> that reads from and writes to a <see cref="PipelineDataBag"/>.
/// </summary>
public sealed class BuiltInAgentAdapter : IDynamicAgent
{
    private readonly string _agentKey;
    private readonly string _displayName;
    private readonly Func<PipelineDataBag, IProgress<AgentLogEntry>, CancellationToken, Task<PipelineDataBag>> _execute;

    private BuiltInAgentAdapter(string agentKey, string displayName,
        Func<PipelineDataBag, IProgress<AgentLogEntry>, CancellationToken, Task<PipelineDataBag>> execute)
    {
        _agentKey = agentKey;
        _displayName = displayName;
        _execute = execute;
    }

    public string AgentKey => _agentKey;
    public string DisplayName => _displayName;

    public Task<PipelineDataBag> RunAsync(PipelineDataBag input, IProgress<AgentLogEntry> log, CancellationToken ct)
        => _execute(input, log, ct);

    /// <summary>Creates an adapter for the specified built-in agent key.</summary>
    public static BuiltInAgentAdapter CreateAdapter(
        string agentKey,
        AnalystAgent analyst,
        ArchitectAgent architect,
        DeveloperAgent developer,
        ReviewerAgent reviewer,
        TestingAgent tester,
        DocumentationAgent documenter)
    {
        return agentKey switch
        {
            "Analyst" => new BuiltInAgentAdapter("Analyst", "Analyst", async (bag, log, ct) =>
            {
                var spec = bag.OriginalSpec
                    ?? throw new InvalidOperationException("OriginalSpec not set in pipeline data bag.");
                var result = await analyst.RunAsync(spec, log, ct);
                bag.Set("analysis", result);
                return bag;
            }),

            "Architect" => new BuiltInAgentAdapter("Architect", "Architect", async (bag, log, ct) =>
            {
                var analysis = bag.Get<AnalysisResult>("analysis");
                var result = await architect.RunAsync(analysis, log, ct);
                bag.Set("blueprint", result);
                return bag;
            }),

            "Developer" => new BuiltInAgentAdapter("Developer", "Developer", async (bag, log, ct) =>
            {
                var analysis = bag.Get<AnalysisResult>("analysis");
                var blueprint = bag.Get<ArchitectureBlueprint>("blueprint");
                var result = await developer.RunAsync((analysis, blueprint), log, ct);
                bag.Set("code", result);
                return bag;
            }),

            "Reviewer" => new BuiltInAgentAdapter("Reviewer", "Reviewer", async (bag, log, ct) =>
            {
                var code = bag.Get<GeneratedSolution>("code");
                var analysis = bag.Get<AnalysisResult>("analysis");
                var blueprint = bag.Get<ArchitectureBlueprint>("blueprint");
                var result = await reviewer.RunAsync((code, analysis, blueprint), log, ct);
                bag.Set("review", result);
                return bag;
            }),

            "Testing" => new BuiltInAgentAdapter("Testing", "Testing", async (bag, log, ct) =>
            {
                var code = bag.Get<GeneratedSolution>("code");
                var analysis = bag.Get<AnalysisResult>("analysis");
                var result = await tester.RunAsync((code, analysis), log, ct);
                bag.Set("tests", result);
                return bag;
            }),

            "Documentation" => new BuiltInAgentAdapter("Documentation", "Documentation", async (bag, log, ct) =>
            {
                var code = bag.Get<GeneratedSolution>("code");
                var blueprint = bag.Get<ArchitectureBlueprint>("blueprint");
                var result = await documenter.RunAsync((code, blueprint), log, ct);
                bag.Set("docs", result);
                return bag;
            }),

            _ => throw new ArgumentException($"Unknown built-in agent key: '{agentKey}'", nameof(agentKey))
        };
    }
}
