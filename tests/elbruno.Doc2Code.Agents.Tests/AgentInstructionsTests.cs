namespace elbruno.Doc2Code.Agents.Tests;

using elbruno.Doc2Code.Core.Prompts;
using FluentAssertions;

public class AgentInstructionsTests
{
    [Fact]
    public void ForAnalyst_ContainsJsonOutputDirective()
    {
        AgentInstructions.ForAnalyst.Should().Contain("JSON");
    }

    [Fact]
    public void ForArchitect_MentionsDotNet()
    {
        AgentInstructions.ForArchitect.Should().Contain(".NET");
    }

    [Fact]
    public void ForDeveloper_MentionsArtifacts()
    {
        AgentInstructions.ForDeveloper.Should().Contain("artifacts");
    }

    [Fact]
    public void ForReviewer_MentionsScoring()
    {
        AgentInstructions.ForReviewer.Should().Contain("scoreOutOf100");
    }

    [Fact]
    public void ForTesting_MentionsXUnit()
    {
        AgentInstructions.ForTesting.Should().Contain("xUnit");
    }

    [Fact]
    public void ForDocumentation_MentionsMermaid()
    {
        AgentInstructions.ForDocumentation.Should().Contain("Mermaid");
    }

    [Fact]
    public void AllAgentNames_AreDistinct()
    {
        var agents = new[]
        {
            new AnalystAgent(null!).DisplayName,
            new ArchitectAgent(null!).DisplayName,
            new DeveloperAgent(null!).DisplayName,
            new ReviewerAgent(null!).DisplayName,
            new TestingAgent(null!).DisplayName,
            new DocumentationAgent(null!).DisplayName
        };

        agents.Should().OnlyHaveUniqueItems();
    }
}

