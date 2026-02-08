// elbruno.Doc2Code — central registry of all agent tools with enable/disable filtering.
namespace elbruno.Doc2Code.Tools;

using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

/// <summary>
/// Holds all discovered tools (MCP + local stubs) and filters them based on the
/// enabled/disabled toggles persisted in <c>Doc2CodeConfig.EnabledTools</c>.
/// </summary>
public sealed class ToolRegistry
{
    private readonly MicrosoftLearnMcpClient _mcpClient;
    private readonly ILogger<ToolRegistry> _logger;
    private readonly Dictionary<string, AITool> _allTools = new(StringComparer.OrdinalIgnoreCase);

    public ToolRegistry(MicrosoftLearnMcpClient mcpClient, ILogger<ToolRegistry> logger)
    {
        _mcpClient = mcpClient;
        _logger = logger;
    }

    /// <summary>
    /// Connects the MCP server and merges MCP tools with local stub tools.
    /// Failures are logged and swallowed so the pipeline can still run without tools.
    /// </summary>
    public async Task InitializeAsync(CancellationToken ct = default)
    {
        // Register local stub tools first
        foreach (var stub in StubToolDefinitions.CreateAll())
        {
            _allTools[stub.Name] = stub;
            _logger.LogDebug("Registered stub tool: {ToolName}", stub.Name);
        }

        // Attempt MCP connection — non-fatal if it fails
        try
        {
            await _mcpClient.InitializeAsync(ct);
            foreach (var tool in _mcpClient.GetAvailableTools())
            {
                _allTools[tool.Name] = tool;
                _logger.LogInformation("Registered MCP tool: {ToolName}", tool.Name);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "MCP connection to Microsoft Learn failed — MCP tools unavailable");
        }
    }

    /// <summary>
    /// Returns only tools whose key is present and <c>true</c> in the toggles map.
    /// </summary>
    public IList<AITool> ResolveEnabledTools(Dictionary<string, bool> toggles)
    {
        var enabled = new List<AITool>();
        foreach (var (key, tool) in _allTools)
        {
            if (toggles.TryGetValue(key, out var on) && on)
                enabled.Add(tool);
        }
        return enabled;
    }

    /// <summary>Returns all known tool key names with their descriptions.</summary>
    public IReadOnlyDictionary<string, string> ListAllToolKeys()
        => _allTools.ToDictionary(
            kvp => kvp.Key,
            kvp => kvp.Value.Description ?? kvp.Value.Name,
            StringComparer.OrdinalIgnoreCase);
}
