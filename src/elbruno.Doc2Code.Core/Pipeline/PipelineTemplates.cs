// elbruno.Doc2Code — pre-built pipeline templates.
namespace elbruno.Doc2Code.Core.Pipeline;

using elbruno.Doc2Code.Core.Models;

/// <summary>
/// Factory for the 4 built-in pipeline templates.
/// </summary>
public static class PipelineTemplates
{
    /// <summary>Returns all 5 pre-built pipeline templates (including blank).</summary>
    public static List<PipelineDefinition> CreateAll() =>
    [
        CreateBlank(),
        CreateDefault(),
        CreateCodeOnly(),
        CreateQuickPrototype(),
        CreateFullQA()
    ];

    /// <summary>Blank pipeline: only the two bookend nodes (Document Input → Generated Assets).</summary>
    public static PipelineDefinition CreateBlank()
    {
        var docInput = MakeDocumentInputStep(100, 200);
        var assets = MakeGeneratedAssetsStep(500, 200);

        return new PipelineDefinition
        {
            Id = "blank-pipeline",
            Name = "Blank",
            Description = "Empty canvas with only the start and end nodes — build your own pipeline from scratch.",
            Steps = [docInput, assets],
            Edges =
            [
                new PipelineEdge { SourceStepId = docInput.StepId, TargetStepId = assets.StepId, OutputKeyMapping = "spec" },
            ]
        };
    }

    // ── Bookend step helpers ────────────────────────────────────────

    private static PipelineStepDefinition MakeDocumentInputStep(double x = 20, double y = 200) => new()
    {
        StepId = PipelineStepDefinition.DocumentInputStepId,
        AgentKey = BuiltInAgentDefinitions.DocumentInputKey,
        PositionX = x,
        PositionY = y,
        IsBookend = true
    };

    private static PipelineStepDefinition MakeGeneratedAssetsStep(double x = 1100, double y = 200) => new()
    {
        StepId = PipelineStepDefinition.GeneratedAssetsStepId,
        AgentKey = BuiltInAgentDefinitions.GeneratedAssetsKey,
        PositionX = x,
        PositionY = y,
        IsBookend = true
    };

    /// <summary>Default pipeline: DocInput → Analyst → Architect → Developer ↔ Reviewer → Testing ∥ Documentation → Assets.</summary>
    public static PipelineDefinition CreateDefault()
    {
        var docInput = MakeDocumentInputStep(20, 200);
        var analyst = new PipelineStepDefinition { StepId = "step-analyst", AgentKey = "Analyst", PositionX = 200, PositionY = 200 };
        var architect = new PipelineStepDefinition { StepId = "step-architect", AgentKey = "Architect", PositionX = 400, PositionY = 200 };
        var developer = new PipelineStepDefinition { StepId = "step-developer", AgentKey = "Developer", PositionX = 600, PositionY = 200 };
        var reviewer = new PipelineStepDefinition
        {
            StepId = "step-reviewer",
            AgentKey = "Reviewer",
            PositionX = 800,
            PositionY = 200,
            RetryPolicy = new StepRetryPolicy { MaxRetries = 2, QualityGateField = "scoreOutOf100", AcceptanceThreshold = 70 }
        };
        var testing = new PipelineStepDefinition { StepId = "step-testing", AgentKey = "Testing", PositionX = 1000, PositionY = 100 };
        var docs = new PipelineStepDefinition { StepId = "step-docs", AgentKey = "Documentation", PositionX = 1000, PositionY = 300 };
        var assets = MakeGeneratedAssetsStep(1200, 200);

        return new PipelineDefinition
        {
            Id = "default-pipeline",
            Name = "Default",
            Description = "Full pipeline: DocInput → Analyst → Architect → Developer ↔ Reviewer → Testing ∥ Documentation → Assets",
            IsDefault = true,
            IsActive = true,
            Steps = [docInput, analyst, architect, developer, reviewer, testing, docs, assets],
            Edges =
            [
                // DocInput → Analyst
                new PipelineEdge { SourceStepId = docInput.StepId, TargetStepId = "step-analyst", OutputKeyMapping = "spec" },
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
                // Terminal steps → Assets
                new PipelineEdge { SourceStepId = "step-testing", TargetStepId = assets.StepId, OutputKeyMapping = "tests" },
                new PipelineEdge { SourceStepId = "step-docs", TargetStepId = assets.StepId, OutputKeyMapping = "docs" },
                new PipelineEdge { SourceStepId = "step-developer", TargetStepId = assets.StepId, OutputKeyMapping = "code" },
            ]
        };
    }

    /// <summary>Code Only pipeline: DocInput → Analyst → Developer → Assets.</summary>
    public static PipelineDefinition CreateCodeOnly()
    {
        var docInput = MakeDocumentInputStep(20, 200);
        var analyst = new PipelineStepDefinition { StepId = "step-analyst", AgentKey = "Analyst", PositionX = 200, PositionY = 200 };
        var developer = new PipelineStepDefinition { StepId = "step-developer", AgentKey = "Developer", PositionX = 400, PositionY = 200 };
        var assets = MakeGeneratedAssetsStep(600, 200);

        return new PipelineDefinition
        {
            Id = "code-only-pipeline",
            Name = "Code Only",
            Description = "Minimal pipeline: DocInput → Analyst → Developer → Assets",
            Steps = [docInput, analyst, developer, assets],
            Edges =
            [
                new PipelineEdge { SourceStepId = docInput.StepId, TargetStepId = "step-analyst", OutputKeyMapping = "spec" },
                new PipelineEdge { SourceStepId = "step-analyst", TargetStepId = "step-developer", OutputKeyMapping = "analysis" },
                new PipelineEdge { SourceStepId = "step-developer", TargetStepId = assets.StepId, OutputKeyMapping = "code" },
            ]
        };
    }

    /// <summary>Quick Prototype pipeline: DocInput → Analyst → Developer → Documentation → Assets.</summary>
    public static PipelineDefinition CreateQuickPrototype()
    {
        var docInput = MakeDocumentInputStep(20, 200);
        var analyst = new PipelineStepDefinition { StepId = "step-analyst", AgentKey = "Analyst", PositionX = 200, PositionY = 200 };
        var developer = new PipelineStepDefinition { StepId = "step-developer", AgentKey = "Developer", PositionX = 400, PositionY = 200 };
        var docs = new PipelineStepDefinition { StepId = "step-docs", AgentKey = "Documentation", PositionX = 600, PositionY = 200 };
        var assets = MakeGeneratedAssetsStep(800, 200);

        return new PipelineDefinition
        {
            Id = "quick-prototype-pipeline",
            Name = "Quick Prototype",
            Description = "Fast pipeline: DocInput → Analyst → Developer → Documentation → Assets",
            Steps = [docInput, analyst, developer, docs, assets],
            Edges =
            [
                new PipelineEdge { SourceStepId = docInput.StepId, TargetStepId = "step-analyst", OutputKeyMapping = "spec" },
                new PipelineEdge { SourceStepId = "step-analyst", TargetStepId = "step-developer", OutputKeyMapping = "analysis" },
                new PipelineEdge { SourceStepId = "step-developer", TargetStepId = "step-docs", OutputKeyMapping = "code" },
                new PipelineEdge { SourceStepId = "step-analyst", TargetStepId = "step-docs", OutputKeyMapping = "analysis" },
                new PipelineEdge { SourceStepId = "step-docs", TargetStepId = assets.StepId, OutputKeyMapping = "docs" },
                new PipelineEdge { SourceStepId = "step-developer", TargetStepId = assets.StepId, OutputKeyMapping = "code" },
            ]
        };
    }

    /// <summary>Full QA pipeline: DocInput → Analyst → Architect → Developer ↔ Reviewer (strict) → Testing → Documentation → Assets.</summary>
    public static PipelineDefinition CreateFullQA()
    {
        var docInput = MakeDocumentInputStep(20, 200);
        var analyst = new PipelineStepDefinition { StepId = "step-analyst", AgentKey = "Analyst", PositionX = 200, PositionY = 200 };
        var architect = new PipelineStepDefinition { StepId = "step-architect", AgentKey = "Architect", PositionX = 400, PositionY = 200 };
        var developer = new PipelineStepDefinition { StepId = "step-developer", AgentKey = "Developer", PositionX = 600, PositionY = 200 };
        var reviewer = new PipelineStepDefinition
        {
            StepId = "step-reviewer",
            AgentKey = "Reviewer",
            PositionX = 800,
            PositionY = 200,
            RetryPolicy = new StepRetryPolicy { MaxRetries = 3, QualityGateField = "scoreOutOf100", AcceptanceThreshold = 80 }
        };
        var testing = new PipelineStepDefinition { StepId = "step-testing", AgentKey = "Testing", PositionX = 1000, PositionY = 200 };
        var docs = new PipelineStepDefinition { StepId = "step-docs", AgentKey = "Documentation", PositionX = 1200, PositionY = 200 };
        var assets = MakeGeneratedAssetsStep(1400, 200);

        return new PipelineDefinition
        {
            Id = "full-qa-pipeline",
            Name = "Full QA",
            Description = "Rigorous pipeline: DocInput → Analyst → Architect → Developer ↔ Reviewer (80, 3×) → Testing → Documentation → Assets",
            Steps = [docInput, analyst, architect, developer, reviewer, testing, docs, assets],
            Edges =
            [
                new PipelineEdge { SourceStepId = docInput.StepId, TargetStepId = "step-analyst", OutputKeyMapping = "spec" },
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
                // Terminal → Assets
                new PipelineEdge { SourceStepId = "step-docs", TargetStepId = assets.StepId, OutputKeyMapping = "docs" },
                new PipelineEdge { SourceStepId = "step-testing", TargetStepId = assets.StepId, OutputKeyMapping = "tests" },
                new PipelineEdge { SourceStepId = "step-developer", TargetStepId = assets.StepId, OutputKeyMapping = "code" },
            ]
        };
    }
}
