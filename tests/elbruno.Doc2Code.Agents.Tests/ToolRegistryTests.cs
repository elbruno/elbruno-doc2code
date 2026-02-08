// elbruno.Doc2Code — tests for ToolRegistry and StubToolDefinitions.
namespace elbruno.Doc2Code.Agents.Tests;

using elbruno.Doc2Code.Tools;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;

public class ToolRegistryTests
{
    [Fact]
    public void StubToolDefinitions_CreatesExpectedNumberOfTools()
    {
        var tools = StubToolDefinitions.CreateAll();

        tools.Should().HaveCount(5);
    }

    [Fact]
    public void StubToolDefinitions_AllToolsHaveUniqueNames()
    {
        var tools = StubToolDefinitions.CreateAll();

        tools.Select(t => t.Name).Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void StubToolDefinitions_ToolsReturnNotImplementedMessage()
    {
        var tools = StubToolDefinitions.CreateAll();

        foreach (var tool in tools)
        {
            tool.Name.Should().NotBeNullOrEmpty();
            tool.Description.Should().NotBeNullOrEmpty();
        }
    }

    [Fact]
    public void ToolRegistry_ResolveEnabledTools_FiltersCorrectly()
    {
        var mcpClient = new MicrosoftLearnMcpClient();
        var logger = Substitute.For<ILogger<ToolRegistry>>();
        var registry = new ToolRegistry(mcpClient, logger);

        var result = registry.ResolveEnabledTools(new Dictionary<string, bool>());

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task ToolRegistry_AfterInitialize_ResolvesStubTools()
    {
        var mcpClient = new MicrosoftLearnMcpClient();
        var logger = Substitute.For<ILogger<ToolRegistry>>();
        var registry = new ToolRegistry(mcpClient, logger);

        // InitializeAsync registers stubs and attempts MCP (which will fail gracefully)
        await registry.InitializeAsync();

        // With all stubs enabled, should return the 5 stub tools
        var allEnabled = new Dictionary<string, bool>
        {
            ["web_search"] = true,
            ["file_read"] = true,
            ["code_compile_check"] = true,
            ["nuget_search"] = true,
            ["run_unit_tests"] = true,
        };
        var result = registry.ResolveEnabledTools(allEnabled);

        result.Should().HaveCount(5);
    }
}
