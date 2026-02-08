// elbruno.Doc2Code — Documentation agent: generates README, architecture guide, and diagrams.
namespace elbruno.Doc2Code.Agents;

using System.Text.Json;
using elbruno.Doc2Code.Core.Prompts;
using elbruno.Doc2Code.Tools;
using elbruno.Doc2Code.Core.Models;
using Microsoft.Extensions.AI;

public sealed class DocumentationAgent
    : LlmAgentBase<(GeneratedSolution Code, ArchitectureBlueprint Blueprint), DocumentationBundle>
{
    private static readonly JsonSerializerOptions s_opts = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public DocumentationAgent(IChatClient chat, ToolRegistry? toolRegistry = null) : base(chat, toolRegistry) { }

    public override string DisplayName => "Documentation";

    protected override string SystemInstruction => AgentInstructions.ForDocumentation;

    protected override string ComposeUserMessage(
        (GeneratedSolution Code, ArchitectureBlueprint Blueprint) payload)
    {
        var envelope = new { solution = payload.Code, blueprint = payload.Blueprint };
        return JsonSerializer.Serialize(envelope, s_opts);
    }

    protected override DocumentationBundle ParseResponse(string json)
        => Deserialize<DocumentationBundle>(json);
}
