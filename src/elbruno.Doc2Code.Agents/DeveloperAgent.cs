// elbruno.Doc2Code — Developer agent: generates all C# source files for the solution.
namespace elbruno.Doc2Code.Agents;

using System.Text.Json;
using elbruno.Doc2Code.Core.Prompts;
using elbruno.Doc2Code.Tools;
using elbruno.Doc2Code.Core.Models;
using Microsoft.Extensions.AI;

/// <summary>
/// Takes the analysis + blueprint pair and produces a full <see cref="GeneratedSolution"/>
/// containing every source file the target project needs.
/// </summary>
public sealed class DeveloperAgent
    : LlmAgentBase<(AnalysisResult Analysis, ArchitectureBlueprint Blueprint), GeneratedSolution>
{
    private static readonly JsonSerializerOptions s_opts = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public DeveloperAgent(IChatClient chat, ToolRegistry? toolRegistry = null) : base(chat, toolRegistry) { }

    public override string DisplayName => "Developer";

    protected override string SystemInstruction => AgentInstructions.ForDeveloper;

    protected override string ComposeUserMessage(
        (AnalysisResult Analysis, ArchitectureBlueprint Blueprint) payload)
    {
        var combined = new
        {
            analysis = payload.Analysis,
            blueprint = payload.Blueprint
        };
        return JsonSerializer.Serialize(combined, s_opts);
    }

    protected override GeneratedSolution ParseResponse(string json)
        => Deserialize<GeneratedSolution>(json);
}
