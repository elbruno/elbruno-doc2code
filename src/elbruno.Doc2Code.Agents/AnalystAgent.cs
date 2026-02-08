// elbruno.Doc2Code — Analyst agent: extracts entities, actors, rules, state machines from requirements.
namespace elbruno.Doc2Code.Agents;

using elbruno.Doc2Code.Core.Prompts;
using elbruno.Doc2Code.Tools;
using elbruno.Doc2Code.Core.Models;
using Microsoft.Extensions.AI;

public sealed class AnalystAgent : LlmAgentBase<RequirementsDocument, AnalysisResult>
{
    public AnalystAgent(IChatClient chat, ToolRegistry? toolRegistry = null) : base(chat, toolRegistry) { }

    public override string DisplayName => "Analyst";

    protected override string SystemInstruction => AgentInstructions.ForAnalyst;

    protected override string ComposeUserMessage(RequirementsDocument payload)
        => $"Document: {payload.SourceFileName}\n\n{payload.ExtractedText}";

    protected override AnalysisResult ParseResponse(string json)
        => Deserialize<AnalysisResult>(json);
}
