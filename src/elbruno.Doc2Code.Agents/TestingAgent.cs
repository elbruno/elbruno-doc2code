// elbruno.Doc2Code — Testing agent: generates xUnit test files for the solution.
namespace elbruno.Doc2Code.Agents;

using System.Text.Json;
using elbruno.Doc2Code.Core.Prompts;
using elbruno.Doc2Code.Tools;
using elbruno.Doc2Code.Core.Models;
using Microsoft.Extensions.AI;

public sealed class TestingAgent
    : LlmAgentBase<(GeneratedSolution Code, AnalysisResult Analysis), TestSuite>
{
    private static readonly JsonSerializerOptions s_opts = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public TestingAgent(IChatClient chat, ToolRegistry? toolRegistry = null) : base(chat, toolRegistry) { }

    public override string DisplayName => "Testing";

    protected override string SystemInstruction => AgentInstructions.ForTesting;

    protected override string ComposeUserMessage(
        (GeneratedSolution Code, AnalysisResult Analysis) payload)
    {
        var envelope = new { solution = payload.Code, analysis = payload.Analysis };
        return JsonSerializer.Serialize(envelope, s_opts);
    }

    protected override TestSuite ParseResponse(string json)
        => Deserialize<TestSuite>(json);
}
