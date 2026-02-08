// elbruno.Doc2Code — MCP client for the Microsoft Learn documentation server.
namespace elbruno.Doc2Code.Tools;

using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Client;

/// <summary>
/// Connects to the Microsoft Learn MCP Server and exposes its tools as <see cref="AITool"/> instances.
/// Tools include: microsoft_docs_search, microsoft_docs_fetch, microsoft_code_sample_search.
/// </summary>
public sealed class MicrosoftLearnMcpClient : IAsyncDisposable
{
    private readonly ILoggerFactory? _loggerFactory;
    private McpClient? _client;
    private IList<McpClientTool> _tools = [];

    public MicrosoftLearnMcpClient(ILoggerFactory? loggerFactory = null)
    {
        _loggerFactory = loggerFactory;
    }

    /// <summary>Creates the MCP client connection and fetches the tool list.</summary>
    public async Task InitializeAsync(CancellationToken ct = default)
    {
        var transport = new HttpClientTransport(new HttpClientTransportOptions
        {
            Endpoint = new Uri("https://learn.microsoft.com/api/mcp"),
            TransportMode = HttpTransportMode.StreamableHttp
        });

        _client = await McpClient.CreateAsync(
            transport,
            new McpClientOptions { ClientInfo = new() { Name = "elbruno.Doc2Code", Version = "1.0.0" } },
            _loggerFactory,
            ct);

        _tools = await _client.ListToolsAsync(cancellationToken: ct);
    }

    /// <summary>Returns all tools discovered from the MCP server as <see cref="AITool"/> instances.</summary>
    public IList<AITool> GetAvailableTools()
        => _tools.Cast<AITool>().ToList();

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_client is not null)
            await _client.DisposeAsync();
    }
}
