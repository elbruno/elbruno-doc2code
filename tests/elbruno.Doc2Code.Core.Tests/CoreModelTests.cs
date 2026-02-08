namespace elbruno.Doc2Code.Core.Tests;

using elbruno.Doc2Code.Core.Models;
using elbruno.Doc2Code.Core.Pipeline;
using elbruno.Doc2Code.Core.DTOs;
using FluentAssertions;

public class CoreModelTests
{
    [Fact]
    public void ReviewResult_Acceptable_WhenScoreMeetsBar()
    {
        var review = new ReviewResult { ScoreOutOf100 = 70 };
        review.Acceptable.Should().BeTrue();
    }

    [Fact]
    public void ReviewResult_NotAcceptable_WhenScoreBelowBar()
    {
        var review = new ReviewResult { ScoreOutOf100 = 69 };
        review.Acceptable.Should().BeFalse();
    }

    [Fact]
    public void GeneratedSolution_ArtifactCount_ReflectsListSize()
    {
        var sol = new GeneratedSolution
        {
            SolutionName = "TestSol",
            Artifacts =
            [
                new CodeArtifact { Path = "a.cs", SourceText = "// a" },
                new CodeArtifact { Path = "b.cs", SourceText = "// b" }
            ]
        };
        sol.ArtifactCount.Should().Be(2);
    }

    [Fact]
    public void RequirementsDocument_TextLength_MatchesExtractedText()
    {
        var doc = new RequirementsDocument
        {
            SourceFileName = "spec.txt",
            ExtractedText = "Hello world"
        };
        doc.TextLength.Should().Be(11);
    }

    [Fact]
    public void PipelineContext_RunId_IsNonEmpty()
    {
        var ctx = new PipelineContext();
        ctx.RunId.Should().NotBeNullOrWhiteSpace();
        ctx.RunId.Length.Should().Be(12);
    }

    [Fact]
    public void PipelineContext_ReviewRetryLimit_IsTwo()
    {
        PipelineContext.ReviewRetryLimit.Should().Be(2);
    }

    [Fact]
    public void PipelineStatus_DefaultStage_IsIdle()
    {
        var status = new PipelineStatus { RunId = "abc" };
        status.Stage.Should().Be(WorkflowStage.Idle);
    }

    [Fact]
    public void AgentProfile_DefaultCreativity_IsPointSeven()
    {
        var profile = new AgentProfile { AgentKey = "test" };
        profile.Creativity.Should().BeApproximately(0.7, 0.001);
    }

    [Fact]
    public void AgentLogEntry_ExpandableContent_DefaultsToNull()
    {
        var entry = new AgentLogEntry { SourceAgent = "Test", Text = "hello" };
        entry.ExpandableContent.Should().BeNull();
    }

    [Fact]
    public void AgentLogEntry_IsStreamingChunk_DefaultsToFalse()
    {
        var entry = new AgentLogEntry { SourceAgent = "Test", Text = "hello" };
        entry.IsStreamingChunk.Should().BeFalse();
    }

    [Fact]
    public void AgentLogEntry_WithExpandableContent_PreservesValue()
    {
        var entry = new AgentLogEntry
        {
            SourceAgent = "Analyst",
            Text = "Prompt sent (100 chars)",
            Severity = LogSeverity.Prompt,
            ExpandableContent = "Full prompt text here"
        };
        entry.ExpandableContent.Should().Be("Full prompt text here");
        entry.Severity.Should().Be(LogSeverity.Prompt);
    }

    [Fact]
    public void AgentLogEntry_StreamingChunk_RoundTrips()
    {
        var entry = new AgentLogEntry
        {
            SourceAgent = "Developer",
            Text = "partial response",
            IsStreamingChunk = true
        };
        entry.IsStreamingChunk.Should().BeTrue();
    }

    [Fact]
    public void LogSeverity_Prompt_Exists()
    {
        var prompt = LogSeverity.Prompt;
        prompt.Should().BeDefined();
        ((int)prompt).Should().BeGreaterThan((int)LogSeverity.Failure);
    }

    [Fact]
    public void LlmProviderKind_HasExpectedValues()
    {
        Enum.GetNames<LlmProviderKind>().Should().HaveCount(4);
        LlmProviderKind.LocalOllama.Should().BeDefined();
        LlmProviderKind.LocalFoundryLocal.Should().BeDefined();
        LlmProviderKind.FoundryOpenAI.Should().BeDefined();
        LlmProviderKind.GitHubCopilot.Should().BeDefined();
    }

    [Fact]
    public void Doc2CodeConfig_DefaultProvider_IsLocalOllama()
    {
        var config = new Doc2CodeConfig();
        config.LlmProvider.Should().Be(LlmProviderKind.LocalOllama);
    }

    [Fact]
    public void Doc2CodeConfig_OllamaSettings_HasDefaults()
    {
        var config = new Doc2CodeConfig();
        config.Ollama.Should().NotBeNull();
        config.Ollama.Endpoint.Should().Be("http://localhost:11434");
        config.Ollama.Model.Should().Be("ministral-3");
    }

    [Fact]
    public void Doc2CodeConfig_BackwardCompat_LlmEndpoint_ReadsFromOllama()
    {
        var config = new Doc2CodeConfig();
        config.Ollama.Endpoint = "http://custom:1234";
        config.LlmEndpoint.Should().Be("http://custom:1234");
    }

    [Fact]
    public void Doc2CodeConfig_BackwardCompat_LlmEndpoint_WritesToOllama()
    {
        var config = new Doc2CodeConfig();
        config.LlmEndpoint = "http://other:5678";
        config.Ollama.Endpoint.Should().Be("http://other:5678");
    }

    [Fact]
    public void Doc2CodeConfig_BackwardCompat_PreferredModel_ReadsFromOllama()
    {
        var config = new Doc2CodeConfig();
        config.Ollama.Model = "custom-model";
        config.PreferredModel.Should().Be("custom-model");
    }

    [Fact]
    public void Doc2CodeConfig_BackwardCompat_PreferredModel_WritesToOllama()
    {
        var config = new Doc2CodeConfig();
        config.PreferredModel = "another-model";
        config.Ollama.Model.Should().Be("another-model");
    }

    [Fact]
    public void Doc2CodeConfig_FoundryLocal_HasDefaults()
    {
        var config = new Doc2CodeConfig();
        config.FoundryLocal.Should().NotBeNull();
        config.FoundryLocal.ModelAlias.Should().Be("phi-3.5-mini");
    }

    [Fact]
    public void Doc2CodeConfig_AzureAIInference_HasDefaults()
    {
        var config = new Doc2CodeConfig();
        config.AzureAIInference.Should().NotBeNull();
        config.AzureAIInference.Endpoint.Should().BeEmpty();
        config.AzureAIInference.Model.Should().BeEmpty();
        config.AzureAIInference.ApiKey.Should().BeEmpty();
    }

    [Fact]
    public void Doc2CodeConfig_Serialization_ExcludesLegacyFields()
    {
        var config = new Doc2CodeConfig();
        var json = System.Text.Json.JsonSerializer.Serialize(config);
        // LlmEndpoint and PreferredModel are [JsonIgnore], should not be in the JSON
        json.Should().NotContain("\"LlmEndpoint\"");
        json.Should().NotContain("\"PreferredModel\"");
    }

    [Fact]
    public void Doc2CodeConfig_Copilot_HasDefaults()
    {
        var config = new Doc2CodeConfig();
        config.Copilot.Should().NotBeNull();
        config.Copilot.Model.Should().Be("gpt-4.1");
        config.Copilot.GitHubToken.Should().BeNull();
        config.Copilot.CliPath.Should().BeNull();
    }

    [Fact]
    public void CopilotSettings_CanOverrideValues()
    {
        var settings = new CopilotSettings
        {
            Model = "claude-sonnet-4.5",
            GitHubToken = "ghp_test123",
            CliPath = "/usr/local/bin/copilot"
        };
        settings.Model.Should().Be("claude-sonnet-4.5");
        settings.GitHubToken.Should().Be("ghp_test123");
        settings.CliPath.Should().Be("/usr/local/bin/copilot");
    }

    [Fact]
    public void ConfigurationDto_Copilot_HasDefaults()
    {
        var dto = new elbruno.Doc2Code.Core.DTOs.ConfigurationDto();
        dto.Copilot.Should().NotBeNull();
        dto.Copilot.Model.Should().Be("gpt-4.1");
    }

    [Fact]
    public void TestConnectionRequest_DefaultProvider_IsLocalOllama()
    {
        var request = new elbruno.Doc2Code.Core.DTOs.TestConnectionRequest();
        request.Provider.Should().Be(LlmProviderKind.LocalOllama);
    }

    [Fact]
    public void TestConnectionRequest_HasDefaultSettings()
    {
        var request = new elbruno.Doc2Code.Core.DTOs.TestConnectionRequest();
        request.Ollama.Should().NotBeNull();
        request.FoundryLocal.Should().NotBeNull();
        request.AzureAIInference.Should().NotBeNull();
        request.Copilot.Should().NotBeNull();
    }

    [Fact]
    public void TestConnectionResult_DefaultSuccess_IsFalse()
    {
        var result = new elbruno.Doc2Code.Core.DTOs.TestConnectionResult();
        result.Success.Should().BeFalse();
        result.Message.Should().BeEmpty();
    }

    [Fact]
    public void TestConnectionResult_CanSetValues()
    {
        var result = new elbruno.Doc2Code.Core.DTOs.TestConnectionResult
        {
            Success = true,
            Message = "Connection OK"
        };
        result.Success.Should().BeTrue();
        result.Message.Should().Be("Connection OK");
    }

    // --- Phase 1: AgentDefinition, PipelineDefinition, PipelineDataBag, BuiltInAgentDefinitions ---

    [Fact]
    public void BuiltInAgentDefinitions_Create_Returns8Agents()
    {
        var agents = BuiltInAgentDefinitions.Create();
        agents.Should().HaveCount(8);
    }

    [Fact]
    public void BuiltInAgentDefinitions_AllHaveCorrectKeys()
    {
        var agents = BuiltInAgentDefinitions.Create();
        var keys = agents.Select(a => a.AgentKey).ToList();
        keys.Should().BeEquivalentTo(["DocumentInput", "Analyst", "Architect", "Developer", "Reviewer", "Testing", "Documentation", "GeneratedAssets"]);
    }

    [Fact]
    public void BuiltInAgentDefinitions_AllAreMarkedBuiltIn()
    {
        var agents = BuiltInAgentDefinitions.Create();
        agents.Should().AllSatisfy(a => a.IsBuiltIn.Should().BeTrue());
    }

    [Fact]
    public void BuiltInAgentDefinitions_AllHaveNonEmptySystemPrompts_ExceptBookends()
    {
        var agents = BuiltInAgentDefinitions.Create();
        agents.Where(a => !a.IsBookend).Should().AllSatisfy(a => a.SystemPrompt.Should().NotBeNullOrWhiteSpace());
        agents.Where(a => a.IsBookend).Should().AllSatisfy(a => a.SystemPrompt.Should().BeEmpty());
    }

    [Fact]
    public void AgentDefinition_DefaultValues()
    {
        var def = new AgentDefinition { AgentKey = "test" };
        def.ModelId.Should().Be("ministral-3");
        def.Temperature.Should().BeApproximately(0.7, 0.001);
    }

    [Fact]
    public void PipelineDefinition_DefaultValues()
    {
        var def = new PipelineDefinition();
        def.Id.Should().NotBeNullOrWhiteSpace();
        def.Version.Should().Be(1);
        def.Steps.Should().BeEmpty();
        def.Edges.Should().BeEmpty();
    }

    [Fact]
    public void PipelineDataBag_SetAndGet_RoundTrips()
    {
        var bag = new PipelineDataBag();
        bag.Set("greeting", "hello");
        var value = bag.Get<string>("greeting");
        value.Should().Be("hello");
    }

    [Fact]
    public void PipelineDataBag_TryGet_ReturnsFalseForMissingKey()
    {
        var bag = new PipelineDataBag();
        var found = bag.TryGet<string>("missing", out _);
        found.Should().BeFalse();
    }

    [Fact]
    public void PipelineDataBag_ContainsKey_ReturnsTrueAfterSet()
    {
        var bag = new PipelineDataBag();
        bag.Set("key1", 42);
        bag.ContainsKey("key1").Should().BeTrue();
    }

    [Fact]
    public void PipelineDataBag_Keys_ReturnsSetKeys()
    {
        var bag = new PipelineDataBag();
        bag.Set("alpha", 1);
        bag.Set("beta", 2);
        bag.Keys.Should().Contain("alpha").And.Contain("beta");
    }

    [Fact]
    public void PipelineDataBag_Get_ThrowsForMissingKey()
    {
        var bag = new PipelineDataBag();
        var act = () => bag.Get<string>("nope");
        act.Should().Throw<KeyNotFoundException>();
    }

    [Fact]
    public void PipelineDataBag_OriginalSpec_DefaultsToNull()
    {
        var bag = new PipelineDataBag();
        bag.OriginalSpec.Should().BeNull();
    }

    // --- Phase 2: TopologicalSorter, PipelineValidator ---

    [Fact]
    public void TopologicalSorter_LinearChain_ReturnsCorrectLevels()
    {
        var pipeline = new PipelineDefinition
        {
            Steps =
            [
                new PipelineStepDefinition { StepId = "A", AgentKey = "Analyst" },
                new PipelineStepDefinition { StepId = "B", AgentKey = "Architect" },
                new PipelineStepDefinition { StepId = "C", AgentKey = "Developer" }
            ],
            Edges =
            [
                new PipelineEdge { SourceStepId = "A", TargetStepId = "B" },
                new PipelineEdge { SourceStepId = "B", TargetStepId = "C" }
            ]
        };
        var sorter = new TopologicalSorter();
        var levels = sorter.Sort(pipeline);
        levels.Should().HaveCount(3);
        levels.Should().AllSatisfy(l => l.Should().HaveCount(1));
    }

    [Fact]
    public void TopologicalSorter_ParallelBranches_GroupsTogether()
    {
        var pipeline = new PipelineDefinition
        {
            Steps =
            [
                new PipelineStepDefinition { StepId = "A", AgentKey = "Analyst" },
                new PipelineStepDefinition { StepId = "B", AgentKey = "Architect" },
                new PipelineStepDefinition { StepId = "C", AgentKey = "Developer" }
            ],
            Edges =
            [
                new PipelineEdge { SourceStepId = "A", TargetStepId = "B" },
                new PipelineEdge { SourceStepId = "A", TargetStepId = "C" }
            ]
        };
        var sorter = new TopologicalSorter();
        var levels = sorter.Sort(pipeline);
        levels.Should().HaveCount(2);
        levels[0].Should().HaveCount(1);
        levels[0][0].StepId.Should().Be("A");
        levels[1].Select(s => s.StepId).Should().BeEquivalentTo(["B", "C"]);
    }

    [Fact]
    public void TopologicalSorter_DiamondShape_ReturnsCorrectLevels()
    {
        var pipeline = new PipelineDefinition
        {
            Steps =
            [
                new PipelineStepDefinition { StepId = "A", AgentKey = "Analyst" },
                new PipelineStepDefinition { StepId = "B", AgentKey = "Architect" },
                new PipelineStepDefinition { StepId = "C", AgentKey = "Developer" },
                new PipelineStepDefinition { StepId = "D", AgentKey = "Reviewer" }
            ],
            Edges =
            [
                new PipelineEdge { SourceStepId = "A", TargetStepId = "B" },
                new PipelineEdge { SourceStepId = "A", TargetStepId = "C" },
                new PipelineEdge { SourceStepId = "B", TargetStepId = "D" },
                new PipelineEdge { SourceStepId = "C", TargetStepId = "D" }
            ]
        };
        var sorter = new TopologicalSorter();
        var levels = sorter.Sort(pipeline);
        levels.Should().HaveCount(3);
        levels[0].Select(s => s.StepId).Should().BeEquivalentTo(["A"]);
        levels[1].Select(s => s.StepId).Should().BeEquivalentTo(["B", "C"]);
        levels[2].Select(s => s.StepId).Should().BeEquivalentTo(["D"]);
    }

    [Fact]
    public void TopologicalSorter_CycleDetection_Throws()
    {
        var pipeline = new PipelineDefinition
        {
            Steps =
            [
                new PipelineStepDefinition { StepId = "A", AgentKey = "Analyst" },
                new PipelineStepDefinition { StepId = "B", AgentKey = "Architect" }
            ],
            Edges =
            [
                new PipelineEdge { SourceStepId = "A", TargetStepId = "B" },
                new PipelineEdge { SourceStepId = "B", TargetStepId = "A" }
            ]
        };
        var sorter = new TopologicalSorter();
        var act = () => sorter.Sort(pipeline);
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void TopologicalSorter_EmptyPipeline_ReturnsEmpty()
    {
        var pipeline = new PipelineDefinition();
        var sorter = new TopologicalSorter();
        var levels = sorter.Sort(pipeline);
        levels.Should().BeEmpty();
    }

    [Fact]
    public void PipelineValidator_ValidPipeline_ReturnsValid()
    {
        var agents = BuiltInAgentDefinitions.Create();
        var pipeline = new PipelineDefinition
        {
            Steps =
            [
                new PipelineStepDefinition { StepId = PipelineStepDefinition.DocumentInputStepId, AgentKey = "DocumentInput", IsBookend = true },
                new PipelineStepDefinition { StepId = "S1", AgentKey = "Analyst" },
                new PipelineStepDefinition { StepId = "S2", AgentKey = "Architect" },
                new PipelineStepDefinition { StepId = PipelineStepDefinition.GeneratedAssetsStepId, AgentKey = "GeneratedAssets", IsBookend = true }
            ],
            Edges =
            [
                new PipelineEdge { SourceStepId = PipelineStepDefinition.DocumentInputStepId, TargetStepId = "S1", OutputKeyMapping = "spec" },
                new PipelineEdge { SourceStepId = "S1", TargetStepId = "S2", OutputKeyMapping = "analysis" },
                new PipelineEdge { SourceStepId = "S2", TargetStepId = PipelineStepDefinition.GeneratedAssetsStepId, OutputKeyMapping = "blueprint" }
            ]
        };
        var validator = new PipelineValidator();
        var result = validator.Validate(pipeline, agents);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void PipelineValidator_MissingAgent_ReturnsError()
    {
        var agents = BuiltInAgentDefinitions.Create();
        var pipeline = new PipelineDefinition
        {
            Steps =
            [
                new PipelineStepDefinition { StepId = "S1", AgentKey = "NonExistent" }
            ]
        };
        var validator = new PipelineValidator();
        var result = validator.Validate(pipeline, agents);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().NotBeEmpty();
    }

    [Fact]
    public void PipelineValidator_CycleDetection_ReturnsError()
    {
        var agents = BuiltInAgentDefinitions.Create();
        var pipeline = new PipelineDefinition
        {
            Steps =
            [
                new PipelineStepDefinition { StepId = "S1", AgentKey = "Analyst" },
                new PipelineStepDefinition { StepId = "S2", AgentKey = "Architect" }
            ],
            Edges =
            [
                new PipelineEdge { SourceStepId = "S1", TargetStepId = "S2" },
                new PipelineEdge { SourceStepId = "S2", TargetStepId = "S1" }
            ]
        };
        var validator = new PipelineValidator();
        var result = validator.Validate(pipeline, agents);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().NotBeEmpty();
    }

    [Fact]
    public void PipelineValidator_DuplicateStepIds_ReturnsError()
    {
        var agents = BuiltInAgentDefinitions.Create();
        var pipeline = new PipelineDefinition
        {
            Steps =
            [
                new PipelineStepDefinition { StepId = "S1", AgentKey = "Analyst" },
                new PipelineStepDefinition { StepId = "S1", AgentKey = "Architect" }
            ]
        };
        var validator = new PipelineValidator();
        // Duplicate step IDs cause an ArgumentException during validation
        var act = () => validator.Validate(pipeline, agents);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void PipelineValidator_EmptyPipeline_ReturnsError()
    {
        var agents = BuiltInAgentDefinitions.Create();
        var pipeline = new PipelineDefinition();
        var validator = new PipelineValidator();
        var result = validator.Validate(pipeline, agents);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().NotBeEmpty();
    }

    // --- Pipeline Templates ---

    [Fact]
    public void PipelineTemplates_CreateAll_Returns5Templates()
    {
        var templates = PipelineTemplates.CreateAll();
        templates.Should().HaveCount(5);
    }

    [Fact]
    public void PipelineTemplates_Blank_HasOnly2BookendSteps()
    {
        var blank = PipelineTemplates.CreateBlank();
        blank.Steps.Should().HaveCount(2);
        blank.Steps.Should().OnlyContain(s => s.IsBookend);
        blank.Edges.Should().HaveCount(1);
    }

    [Fact]
    public void PipelineTemplates_Default_IsMarkedDefaultAndActive()
    {
        var def = PipelineTemplates.CreateDefault();
        def.IsDefault.Should().BeTrue();
        def.IsActive.Should().BeTrue();
    }

    [Fact]
    public void PipelineTemplates_Default_Has8Steps()
    {
        var def = PipelineTemplates.CreateDefault();
        def.Steps.Should().HaveCount(8);
    }

    // --- Bookend Steps ---

    [Fact]
    public void BuiltInAgentDefinitions_Has2Bookends()
    {
        var agents = BuiltInAgentDefinitions.Create();
        var bookends = agents.Where(a => a.IsBookend).ToList();
        bookends.Should().HaveCount(2);
        bookends.Select(a => a.AgentKey).Should().BeEquivalentTo(["DocumentInput", "GeneratedAssets"]);
    }

    [Fact]
    public void PipelineTemplates_AllTemplatesHaveBookendSteps()
    {
        var templates = PipelineTemplates.CreateAll();
        foreach (var t in templates)
        {
            t.Steps.Should().Contain(s => s.StepId == PipelineStepDefinition.DocumentInputStepId,
                because: $"template '{t.Name}' must have a Document Input bookend");
            t.Steps.Should().Contain(s => s.StepId == PipelineStepDefinition.GeneratedAssetsStepId,
                because: $"template '{t.Name}' must have a Generated Assets bookend");
        }
    }

    [Fact]
    public void PipelineTemplates_BookendSteps_AreMarkedIsBookend()
    {
        var def = PipelineTemplates.CreateDefault();
        var docInput = def.Steps.First(s => s.StepId == PipelineStepDefinition.DocumentInputStepId);
        var assets = def.Steps.First(s => s.StepId == PipelineStepDefinition.GeneratedAssetsStepId);
        docInput.IsBookend.Should().BeTrue();
        assets.IsBookend.Should().BeTrue();
    }

    [Fact]
    public void PipelineValidator_MissingBookends_ReturnsErrors()
    {
        var agents = BuiltInAgentDefinitions.Create();
        var pipeline = new PipelineDefinition
        {
            Steps = [new PipelineStepDefinition { StepId = "S1", AgentKey = "Analyst" }]
        };
        var validator = new PipelineValidator();
        var result = validator.Validate(pipeline, agents);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("Document Input"));
        result.Errors.Should().Contain(e => e.Contains("Generated Assets"));
    }

    [Fact]
    public void PipelineStepDefinition_IsBookend_DefaultsFalse()
    {
        var step = new PipelineStepDefinition();
        step.IsBookend.Should().BeFalse();
    }

    [Fact]
    public void AgentDefinition_IsBookend_DefaultsFalse()
    {
        var def = new AgentDefinition { AgentKey = "test" };
        def.IsBookend.Should().BeFalse();
    }

    // --- SettingsExportBundle ---

    [Fact]
    public void SettingsExportBundle_Serialization_RoundTrips()
    {
        var bundle = new SettingsExportBundle
        {
            Version = 2,
            AgentDefinitions = [new AgentDefinition { AgentKey = "Analyst" }],
            Pipelines = [new PipelineDefinition { Name = "Test" }],
            EnabledTools = new Dictionary<string, bool> { ["tool1"] = true }
        };
        var json = System.Text.Json.JsonSerializer.Serialize(bundle);
        var deserialized = System.Text.Json.JsonSerializer.Deserialize<SettingsExportBundle>(json)!;
        deserialized.Version.Should().Be(2);
        deserialized.AgentDefinitions.Should().HaveCount(1);
        deserialized.AgentDefinitions[0].AgentKey.Should().Be("Analyst");
        deserialized.Pipelines.Should().HaveCount(1);
        deserialized.Pipelines[0].Name.Should().Be("Test");
        deserialized.EnabledTools.Should().ContainKey("tool1").WhoseValue.Should().BeTrue();
    }

    // --- PipelineStatus ---

    [Fact]
    public void PipelineStatus_NewFields_HaveDefaults()
    {
        var status = new PipelineStatus { RunId = "run1" };
        status.StepIndex.Should().Be(0);
        status.TotalSteps.Should().Be(0);
        status.StepAgentKey.Should().BeEmpty();
        status.ParallelPeers.Should().BeEmpty();
    }

    [Fact]
    public void WorkflowStage_Custom_Exists()
    {
        var custom = WorkflowStage.Custom;
        custom.Should().BeDefined();
    }
}

