// elbruno.Doc2Code — pre-built pipeline templates.
namespace elbruno.Doc2Code.Core.Pipeline;

using elbruno.Doc2Code.Core.Models;

/// <summary>
/// Factory for the 4 built-in pipeline templates.
/// </summary>
public static class PipelineTemplates
{
    /// <summary>Returns all 4 pre-built pipeline templates.</summary>
    public static List<PipelineDefinition> CreateAll() =>
    [
        CreateDefault(),
        CreateCodeOnly(),
        CreateQuickPrototype(),
        CreateFullQA()
    ];

    /// <summary>Default pipeline: Analyst → Architect → Developer ↔ Reviewer → Testing ∥ Documentation.</summary>
    public static PipelineDefinition CreateDefault()
    {
        var analyst = new PipelineStepDefinition { StepId = "step-analyst", AgentKey = "Analyst", PositionX = 100, PositionY = 200 };
        var architect = new PipelineStepDefinition { StepId = "step-architect", AgentKey = "Architect", PositionX = 300, PositionY = 200 };
        var developer = new PipelineStepDefinition { StepId = "step-developer", AgentKey = "Developer", PositionX = 500, PositionY = 200 };
        var reviewer = new PipelineStepDefinition
        {
            StepId = "step-reviewer",
            AgentKey = "Reviewer",
            PositionX = 700,
            PositionY = 200,
            RetryPolicy = new StepRetryPolicy { MaxRetries = 2, QualityGateField = "scoreOutOf100", AcceptanceThreshold = 70 }
        };
        var testing = new PipelineStepDefinition { StepId = "step-testing", AgentKey = "Testing", PositionX = 900, PositionY = 100 };
        var docs = new PipelineStepDefinition { StepId = "step-docs", AgentKey = "Documentation", PositionX = 900, PositionY = 300 };

        return new PipelineDefinition
        {
            Id = "default-pipeline",
            Name = "Default",
            Description = "Full 6-agent pipeline: Analyst → Architect → Developer ↔ Reviewer → Testing ∥ Documentation",
            IsDefault = true,
            IsActive = true,
            Steps = [analyst, architect, developer, reviewer, testing, docs],
            Edges =
            [
                new PipelineEdge { SourceStepId = "step-analyst", TargetStepId = "step-architect", OutputKeyMapping = "analysis" },
                new PipelineEdge { SourceStepId = "step-architect", TargetStepId = "step-developer", OutputKeyMapping = "blueprint" },
                new PipelineEdge { SourceStepId = "step-analyst", TargetStepId = "step-developer", OutputKeyMapping = "analysis" },
                new PipelineEdge { SourceStepId = "step-developer", TargetStepId = "step-reviewer", OutputKeyMapping = "code" },
                new PipelineEdge { SourceStepId = "step-analyst", TargetStepId = "step-reviewer", OutputKeyMapping = "analysis" },
                new PipelineEdge { SourceStepId = "step-architect", TargetStepId = "step-reviewer", OutputKeyMapping = "blueprint" },
                new PipelineEdge { SourceStepId = "step-reviewer", TargetStepId = "step-testing", OutputKeyMapping = "review" },
                new PipelineEdge { SourceStepId = "step-developer", TargetStepId = "step-testing", OutputKeyMapping = "code" },
                new PipelineEdge { SourceStepId = "step-analyst", TargetStepId = "step-testing", OutputKeyMapping = "analysis" },
                new PipelineEdge { SourceStepId = "step-reviewer", TargetStepId = "step-docs", OutputKeyMapping = "review" },
                new PipelineEdge { SourceStepId = "step-developer", TargetStepId = "step-docs", OutputKeyMapping = "code" },
                new PipelineEdge { SourceStepId = "step-architect", TargetStepId = "step-docs", OutputKeyMapping = "blueprint" },
            ]
        };
    }

    /// <summary>Code Only pipeline: Analyst → Developer.</summary>
    public static PipelineDefinition CreateCodeOnly()
    {
        var analyst = new PipelineStepDefinition { StepId = "step-analyst", AgentKey = "Analyst", PositionX = 100, PositionY = 200 };
        var developer = new PipelineStepDefinition { StepId = "step-developer", AgentKey = "Developer", PositionX = 300, PositionY = 200 };

        return new PipelineDefinition
        {
            Id = "code-only-pipeline",
            Name = "Code Only",
            Description = "Minimal pipeline: Analyst → Developer",
            Steps = [analyst, developer],
            Edges =
            [
                new PipelineEdge { SourceStepId = "step-analyst", TargetStepId = "step-developer", OutputKeyMapping = "analysis" },
            ]
        };
    }

    /// <summary>Quick Prototype pipeline: Analyst → Developer → Documentation.</summary>
    public static PipelineDefinition CreateQuickPrototype()
    {
        var analyst = new PipelineStepDefinition { StepId = "step-analyst", AgentKey = "Analyst", PositionX = 100, PositionY = 200 };
        var developer = new PipelineStepDefinition { StepId = "step-developer", AgentKey = "Developer", PositionX = 300, PositionY = 200 };
        var docs = new PipelineStepDefinition { StepId = "step-docs", AgentKey = "Documentation", PositionX = 500, PositionY = 200 };

        return new PipelineDefinition
        {
            Id = "quick-prototype-pipeline",
            Name = "Quick Prototype",
            Description = "Fast pipeline: Analyst → Developer → Documentation",
            Steps = [analyst, developer, docs],
            Edges =
            [
                new PipelineEdge { SourceStepId = "step-analyst", TargetStepId = "step-developer", OutputKeyMapping = "analysis" },
                new PipelineEdge { SourceStepId = "step-developer", TargetStepId = "step-docs", OutputKeyMapping = "code" },
                new PipelineEdge { SourceStepId = "step-analyst", TargetStepId = "step-docs", OutputKeyMapping = "blueprint" },
            ]
        };
    }

    /// <summary>Full QA pipeline: Analyst → Architect → Developer ↔ Reviewer (strict) → Testing → Reviewer → Documentation.</summary>
    public static PipelineDefinition CreateFullQA()
    {
        var analyst = new PipelineStepDefinition { StepId = "step-analyst", AgentKey = "Analyst", PositionX = 100, PositionY = 200 };
        var architect = new PipelineStepDefinition { StepId = "step-architect", AgentKey = "Architect", PositionX = 300, PositionY = 200 };
        var developer = new PipelineStepDefinition { StepId = "step-developer", AgentKey = "Developer", PositionX = 500, PositionY = 200 };
        var reviewer = new PipelineStepDefinition
        {
            StepId = "step-reviewer",
            AgentKey = "Reviewer",
            PositionX = 700,
            PositionY = 200,
            RetryPolicy = new StepRetryPolicy { MaxRetries = 3, QualityGateField = "scoreOutOf100", AcceptanceThreshold = 80 }
        };
        var testing = new PipelineStepDefinition { StepId = "step-testing", AgentKey = "Testing", PositionX = 900, PositionY = 200 };
        var docs = new PipelineStepDefinition { StepId = "step-docs", AgentKey = "Documentation", PositionX = 1300, PositionY = 200 };

        return new PipelineDefinition
        {
            Id = "full-qa-pipeline",
            Name = "Full QA",
            Description = "Rigorous pipeline with strict quality gates: Analyst → Architect → Developer ↔ Reviewer (threshold 80, max 3) → Testing → Documentation",
            Steps = [analyst, architect, developer, reviewer, testing, docs],
            Edges =
            [
                new PipelineEdge { SourceStepId = "step-analyst", TargetStepId = "step-architect", OutputKeyMapping = "analysis" },
                new PipelineEdge { SourceStepId = "step-architect", TargetStepId = "step-developer", OutputKeyMapping = "blueprint" },
                new PipelineEdge { SourceStepId = "step-analyst", TargetStepId = "step-developer", OutputKeyMapping = "analysis" },
                new PipelineEdge { SourceStepId = "step-developer", TargetStepId = "step-reviewer", OutputKeyMapping = "code" },
                new PipelineEdge { SourceStepId = "step-analyst", TargetStepId = "step-reviewer", OutputKeyMapping = "analysis" },
                new PipelineEdge { SourceStepId = "step-architect", TargetStepId = "step-reviewer", OutputKeyMapping = "blueprint" },
                new PipelineEdge { SourceStepId = "step-reviewer", TargetStepId = "step-testing", OutputKeyMapping = "review" },
                new PipelineEdge { SourceStepId = "step-developer", TargetStepId = "step-testing", OutputKeyMapping = "code" },
                new PipelineEdge { SourceStepId = "step-analyst", TargetStepId = "step-testing", OutputKeyMapping = "analysis" },
                new PipelineEdge { SourceStepId = "step-testing", TargetStepId = "step-docs", OutputKeyMapping = "tests" },
                new PipelineEdge { SourceStepId = "step-developer", TargetStepId = "step-docs", OutputKeyMapping = "code" },
                new PipelineEdge { SourceStepId = "step-architect", TargetStepId = "step-docs", OutputKeyMapping = "blueprint" },
            ]
        };
    }
}
