namespace elbruno.Doc2Code.Core.Tests;

using elbruno.Doc2Code.Core.Models;
using elbruno.Doc2Code.Core.Pipeline;
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
}

