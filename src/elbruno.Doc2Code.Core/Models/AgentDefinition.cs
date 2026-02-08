// elbruno.Doc2Code — defines the configuration for a single agent in the pipeline.
namespace elbruno.Doc2Code.Core.Models;

using elbruno.Doc2Code.Core.Prompts;

/// <summary>
/// Complete definition of a pipeline agent, including its prompts, model settings,
/// and I/O schema. Built-in agents have <see cref="IsBuiltIn"/> = true and cannot
/// be deleted or have their key changed.
/// </summary>
public sealed class AgentDefinition
{
    /// <summary>Unique identifier for this agent (e.g. "Analyst", "Architect").</summary>
    public required string AgentKey { get; init; }

    /// <summary>Human-readable name shown in the UI.</summary>
    public string DisplayName { get; set; } = "";

    /// <summary>Whether this agent is one of the 6 built-in pipeline agents.</summary>
    public bool IsBuiltIn { get; init; }

    /// <summary>System prompt sent to the LLM.</summary>
    public string SystemPrompt { get; set; } = "";

    /// <summary>User prompt template with <c>{key}</c> placeholders for bag values.</summary>
    public string UserPromptTemplate { get; set; } = "";

    /// <summary>LLM model identifier override for this agent.</summary>
    public string ModelId { get; set; } = "ministral-3";

    /// <summary>Sampling temperature (0.0 = deterministic, 1.0 = creative).</summary>
    public double Temperature { get; set; } = 0.7;

    /// <summary>Whether this agent can use registered tools.</summary>
    public bool SupportsTools { get; set; }

    /// <summary>Keys this agent expects to find in the <see cref="PipelineDataBag"/>.</summary>
    public List<string> ExpectedInputKeys { get; set; } = [];

    /// <summary>Key under which this agent stores its output in the <see cref="PipelineDataBag"/>.</summary>
    public string OutputKey { get; set; } = "";

    /// <summary>Short description of what this agent does.</summary>
    public string Description { get; set; } = "";
}

/// <summary>Factory for the 6 built-in agent definitions.</summary>
public static class BuiltInAgentDefinitions
{
    /// <summary>Returns the 6 default agent definitions with prompts from <see cref="AgentInstructions"/>.</summary>
    public static List<AgentDefinition> Create() =>
    [
        new AgentDefinition
        {
            AgentKey = "Analyst",
            DisplayName = "Analyst",
            IsBuiltIn = true,
            SystemPrompt = AgentInstructions.ForAnalyst,
            UserPromptTemplate = "Analyze the following requirements specification:\n\n{spec}",
            Description = "Extracts domain entities, actors, rules, and state machines from requirements.",
            ExpectedInputKeys = ["spec"],
            OutputKey = "analysis",
            SupportsTools = true
        },
        new AgentDefinition
        {
            AgentKey = "Architect",
            DisplayName = "Architect",
            IsBuiltIn = true,
            SystemPrompt = AgentInstructions.ForArchitect,
            UserPromptTemplate = "Design a solution architecture based on the following analysis:\n\n{analysis}",
            Description = "Designs solution blueprint with modules, patterns, and references.",
            ExpectedInputKeys = ["analysis"],
            OutputKey = "blueprint",
            SupportsTools = true
        },
        new AgentDefinition
        {
            AgentKey = "Developer",
            DisplayName = "Developer",
            IsBuiltIn = true,
            SystemPrompt = AgentInstructions.ForDeveloper,
            UserPromptTemplate = "Generate all source files based on:\n\nAnalysis:\n{analysis}\n\nBlueprint:\n{blueprint}",
            Description = "Generates C# source files for the .NET 10 solution.",
            ExpectedInputKeys = ["analysis", "blueprint"],
            OutputKey = "code",
            SupportsTools = true
        },
        new AgentDefinition
        {
            AgentKey = "Reviewer",
            DisplayName = "Reviewer",
            IsBuiltIn = true,
            SystemPrompt = AgentInstructions.ForReviewer,
            UserPromptTemplate = "Review the generated code:\n\nCode:\n{code}\n\nAnalysis:\n{analysis}\n\nBlueprint:\n{blueprint}",
            Description = "Reviews code quality; enforces a quality gate (score >= 70 to pass).",
            ExpectedInputKeys = ["code", "analysis", "blueprint"],
            OutputKey = "review",
            SupportsTools = true
        },
        new AgentDefinition
        {
            AgentKey = "Testing",
            DisplayName = "Testing",
            IsBuiltIn = true,
            SystemPrompt = AgentInstructions.ForTesting,
            UserPromptTemplate = "Generate xUnit tests based on:\n\nCode:\n{code}\n\nAnalysis:\n{analysis}",
            Description = "Generates xUnit test files with entity validation and business rule tests.",
            ExpectedInputKeys = ["code", "analysis"],
            OutputKey = "tests",
            SupportsTools = true
        },
        new AgentDefinition
        {
            AgentKey = "Documentation",
            DisplayName = "Documentation",
            IsBuiltIn = true,
            SystemPrompt = AgentInstructions.ForDocumentation,
            UserPromptTemplate = "Generate documentation based on:\n\nCode:\n{code}\n\nBlueprint:\n{blueprint}",
            Description = "Produces README, architecture guide, and Mermaid diagrams.",
            ExpectedInputKeys = ["code", "blueprint"],
            OutputKey = "docs",
            SupportsTools = true
        }
    ];
}
