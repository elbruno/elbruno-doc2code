// elbruno.Doc2Code — IChatClient adapter that delegates to GitHub Copilot SDK via the Microsoft Agent Framework bridge.
namespace elbruno.Doc2Code.LlmProviders;

using System.Runtime.CompilerServices;
using elbruno.Doc2Code.Core.Models;
using GitHub.Copilot.SDK;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

/// <summary>
/// Wraps a GitHub Copilot <see cref="AIAgent"/> (created via <c>CopilotClient.AsAIAgent()</c>)
/// as an <see cref="IChatClient"/> so it can be used seamlessly by the Doc2Code agent pipeline.
/// </summary>
public sealed class CopilotChatClientAdapter : IChatClient
{
    private readonly AIAgent _agent;
    private readonly CopilotClient _copilotClient;
    private readonly ILogger? _logger;
    private AgentSession? _session;
    private bool _disposed;

    private CopilotChatClientAdapter(CopilotClient copilotClient, AIAgent agent, ILogger? logger)
    {
        _copilotClient = copilotClient;
        _agent = agent;
        _logger = logger;
    }

    /// <summary>
    /// Creates and initializes a new <see cref="CopilotChatClientAdapter"/>.
    /// Starts the <see cref="CopilotClient"/> and creates an initial session.
    /// </summary>
    public static async Task<CopilotChatClientAdapter> CreateAsync(
        CopilotSettings settings, ILogger? logger = null, CancellationToken ct = default)
    {
        var options = new CopilotClientOptions();
        if (!string.IsNullOrWhiteSpace(settings.CliPath))
            options.CliPath = settings.CliPath;

        var client = new CopilotClient(options);
        await client.StartAsync(ct);

        var sessionConfig = new SessionConfig
        {
            Model = settings.Model,
            Streaming = true
        };

        var agent = client.AsAIAgent(sessionConfig, ownsClient: false);

        var adapter = new CopilotChatClientAdapter(client, agent, logger);
        adapter._session = await agent.CreateSessionAsync(ct).ConfigureAwait(false);
        return adapter;
    }

    /// <inheritdoc />
    public async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var session = _session ?? await _agent.CreateSessionAsync(cancellationToken).ConfigureAwait(false);
        var agentResponse = await _agent.RunAsync(messages, session, options: null, cancellationToken);

        return new ChatResponse(agentResponse.Messages.Select(m =>
            new ChatMessage(m.Role, m.Text)).ToList());
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages, ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var session = _session ?? await _agent.CreateSessionAsync(cancellationToken).ConfigureAwait(false);

        await foreach (var update in _agent.RunStreamingAsync(messages, session, options: null, cancellationToken))
        {
            yield return new ChatResponseUpdate(update.Role ?? ChatRole.Assistant, update.Text ?? string.Empty);
        }
    }

    /// <inheritdoc />
    public object? GetService(Type serviceType, object? serviceKey = null)
    {
        if (serviceType == typeof(IChatClient) && serviceKey is null)
            return this;
        return null;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        try
        {
            _copilotClient.StopAsync().GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Error stopping CopilotClient during dispose");
        }

        if (_agent is IAsyncDisposable asyncDisposable)
        {
            asyncDisposable.DisposeAsync().AsTask().GetAwaiter().GetResult();
        }
        else if (_agent is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }
}
