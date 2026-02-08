// elbruno.Doc2Code — creates IDynamicAgent instances from AgentDefinition.
namespace elbruno.Doc2Code.Agents;

using elbruno.Doc2Code.Core.Abstractions;
using elbruno.Doc2Code.Core.Models;
using elbruno.Doc2Code.Tools;
using Microsoft.Extensions.AI;

/// <summary>Contract for creating <see cref="IDynamicAgent"/> instances from definitions.</summary>
public interface IAgentFactory
{
    /// <summary>Creates the appropriate agent for the given definition.</summary>
    IDynamicAgent Create(AgentDefinition definition);
}

/// <summary>
/// Creates <see cref="IDynamicAgent"/> instances: built-in agents are wrapped via
/// <see cref="BuiltInAgentAdapter"/>, custom agents use <see cref="DynamicLlmAgent"/>.
/// </summary>
public sealed class AgentFactory : IAgentFactory
{
    private readonly AnalystAgent _analyst;
    private readonly ArchitectAgent _architect;
    private readonly DeveloperAgent _developer;
    private readonly ReviewerAgent _reviewer;
    private readonly TestingAgent _tester;
    private readonly DocumentationAgent _documenter;
    private readonly IChatClient _chat;
    private readonly ToolRegistry? _toolRegistry;

    public AgentFactory(
        AnalystAgent analyst,
        ArchitectAgent architect,
        DeveloperAgent developer,
        ReviewerAgent reviewer,
        TestingAgent tester,
        DocumentationAgent documenter,
        IChatClient chat,
        ToolRegistry? toolRegistry = null)
    {
        _analyst = analyst;
        _architect = architect;
        _developer = developer;
        _reviewer = reviewer;
        _tester = tester;
        _documenter = documenter;
        _chat = chat;
        _toolRegistry = toolRegistry;
    }

    public IDynamicAgent Create(AgentDefinition definition)
    {
        if (definition.IsBuiltIn)
        {
            return BuiltInAgentAdapter.CreateAdapter(
                definition.AgentKey,
                _analyst, _architect, _developer, _reviewer, _tester, _documenter);
        }

        return new DynamicLlmAgent(definition, _chat, _toolRegistry);
    }
}
