// elbruno.Doc2Code — Architect agent: designs the .NET solution structure from analysis output.
namespace elbruno.Doc2Code.Agents;

using System.Text.Json;
using elbruno.Doc2Code.Core.Prompts;
using elbruno.Doc2Code.Tools;
using elbruno.Doc2Code.Core.Models;
using Microsoft.Extensions.AI;

public sealed class ArchitectAgent : LlmAgentBase<AnalysisResult, ArchitectureBlueprint>
{
    private static readonly JsonSerializerOptions s_opts = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public ArchitectAgent(IChatClient chat, ToolRegistry? toolRegistry = null) : base(chat, toolRegistry) { }

    public override string DisplayName => "Architect";

    protected override string SystemInstruction => AgentInstructions.ForArchitect;

    protected override string ComposeUserMessage(AnalysisResult payload)
        => JsonSerializer.Serialize(payload, s_opts);

    protected override ArchitectureBlueprint ParseResponse(string json)
        => Deserialize<ArchitectureBlueprint>(json);
}
