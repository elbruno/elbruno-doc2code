// elbruno.Doc2Code — Reviewer agent: inspects generated code and assigns a quality score.
namespace elbruno.Doc2Code.Agents;

using System.Text.Json;
using elbruno.Doc2Code.Core.Prompts;
using elbruno.Doc2Code.Tools;
using elbruno.Doc2Code.Core.Models;
using Microsoft.Extensions.AI;

/// <summary>
/// Cross-validates the generated solution against the analysis and blueprint,
/// producing a <see cref="ReviewResult"/> with a 0-100 score.  When the score
/// is below <see cref="ReviewResult.AcceptanceBar"/> the pipeline loops back
/// to the Developer agent (up to the retry limit).
/// </summary>
public sealed class ReviewerAgent
    : LlmAgentBase<(GeneratedSolution Code, AnalysisResult Analysis, ArchitectureBlueprint Blueprint), ReviewResult>
{
    private static readonly JsonSerializerOptions s_opts = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public ReviewerAgent(IChatClient chat, ToolRegistry? toolRegistry = null) : base(chat, toolRegistry) { }

    public override string DisplayName => "Reviewer";

    protected override string SystemInstruction => AgentInstructions.ForReviewer;

    protected override string ComposeUserMessage(
        (GeneratedSolution Code, AnalysisResult Analysis, ArchitectureBlueprint Blueprint) payload)
    {
        var envelope = new
        {
            solution = payload.Code,
            analysis = payload.Analysis,
            blueprint = payload.Blueprint
        };
        return JsonSerializer.Serialize(envelope, s_opts);
    }

    protected override ReviewResult ParseResponse(string json)
        => Deserialize<ReviewResult>(json);
}
