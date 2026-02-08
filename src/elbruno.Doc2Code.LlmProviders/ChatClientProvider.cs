// elbruno.Doc2Code — delegating IChatClient wrapper that swaps the inner client based on provider settings.
namespace elbruno.Doc2Code.LlmProviders;

using Azure;
using Azure.AI.Inference;
using elbruno.Doc2Code.Core.Abstractions;
using elbruno.Doc2Code.Core.DTOs;
using elbruno.Doc2Code.Core.Models;
using elbruno.Doc2Code.ServiceDefaults;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

/// <summary>
/// Implements <see cref="IChatClient"/> as a delegating wrapper — holds a volatile inner
/// <see cref="IChatClient"/> and forwards all members to it. Call <see cref="RefreshAsync"/>
/// to rebuild the inner client from the latest <see cref="ISettingsStore"/> configuration.
/// </summary>
public sealed class ChatClientProvider : IChatClient, IRefreshableChatClient
{
    private readonly ISettingsStore _settingsStore;
    private readonly ILogger<ChatClientProvider> _logger;
    private readonly SemaphoreSlim _refreshLock = new(1, 1);
    private volatile IChatClient _inner;

    public ChatClientProvider(ISettingsStore settingsStore, ILogger<ChatClientProvider> logger)
    {
        _settingsStore = settingsStore;
        _logger = logger;
        // Default inner client — will be replaced by RefreshAsync at startup.
        _inner = new OllamaSharp.OllamaApiClient(
            new HttpClient
            {
                BaseAddress = new Uri("http://localhost:11434"),
                Timeout = Doc2CodeTimeouts.LlmHttpClientTimeout
            },
            "ministral-3");
    }

    /// <summary>
    /// Reads the current config from <see cref="ISettingsStore"/> and creates the
    /// appropriate inner <see cref="IChatClient"/> based on <see cref="LlmProviderKind"/>.
    /// </summary>
    public async Task RefreshAsync(CancellationToken ct = default)
    {
        await _refreshLock.WaitAsync(ct);
        try
        {
            var config = await _settingsStore.GetAsync(ct);
            var previous = _inner;

            _inner = config.LlmProvider switch
            {
                LlmProviderKind.LocalOllama => CreateOllamaClient(config.Ollama),
                LlmProviderKind.LocalFoundryLocal => CreateFoundryLocalClient(config.FoundryLocal),
                LlmProviderKind.FoundryOpenAI => CreateAzureAIInferenceClient(config.AzureAIInference),
                LlmProviderKind.GitHubCopilot => await CreateCopilotClientAsync(config.Copilot, ct),
                _ => CreateOllamaClient(config.Ollama)
            };

            _logger.LogInformation("ChatClientProvider refreshed — provider: {Provider}", config.LlmProvider);

            if (previous is IDisposable disposable && !ReferenceEquals(previous, _inner))
            {
                disposable.Dispose();
            }
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    /// <summary>
    /// Creates a temporary <see cref="IChatClient"/> for the requested provider,
    /// sends a minimal probe message, and returns success/failure.
    /// </summary>
    public async Task<TestConnectionResult> TestConnectionAsync(TestConnectionRequest request, CancellationToken ct = default)
    {
        IChatClient? tempClient = null;
        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(15));

            tempClient = request.Provider switch
            {
                LlmProviderKind.LocalOllama => CreateOllamaClient(request.Ollama),
                LlmProviderKind.LocalFoundryLocal => CreateFoundryLocalClient(request.FoundryLocal),
                LlmProviderKind.FoundryOpenAI => CreateAzureAIInferenceClient(request.AzureAIInference),
                LlmProviderKind.GitHubCopilot => await CreateCopilotClientAsync(request.Copilot, cts.Token),
                _ => CreateOllamaClient(request.Ollama)
            };

            var messages = new List<ChatMessage> { new(Microsoft.Extensions.AI.ChatRole.User, "Hi") };
            await tempClient.GetResponseAsync(messages, cancellationToken: cts.Token);
            return new TestConnectionResult { Success = true, Message = "Connection OK" };
        }
        catch (Exception ex)
        {
            var msg = ex.Message;
            if (msg.Length > 500) msg = msg[..500] + "…";
            return new TestConnectionResult { Success = false, Message = $"Connection failed: {msg}" };
        }
        finally
        {
            if (tempClient is IDisposable disposable)
                disposable.Dispose();
        }
    }

    internal static IChatClient CreateOllamaClient(OllamaSettings settings)
    {
        var httpClient = new HttpClient
        {
            BaseAddress = new Uri(settings.Endpoint),
            Timeout = Doc2CodeTimeouts.LlmHttpClientTimeout
        };
        return new OllamaSharp.OllamaApiClient(httpClient, settings.Model);
    }

    internal static IChatClient CreateFoundryLocalClient(FoundryLocalSettings settings)
    {
        // FoundryLocal exposes an OpenAI-compatible endpoint.
        // The user must have FoundryLocal running and expose its endpoint.
        var openAiClient = new OpenAI.OpenAIClient(
            new System.ClientModel.ApiKeyCredential("foundry-local"),
            new OpenAI.OpenAIClientOptions { Endpoint = new Uri(settings.Endpoint) });
        return openAiClient.GetChatClient(settings.ModelAlias).AsIChatClient();
    }

    internal static IChatClient CreateAzureAIInferenceClient(AzureAIInferenceSettings settings)
    {
        var client = new ChatCompletionsClient(
            new Uri(settings.Endpoint),
            new AzureKeyCredential(settings.ApiKey));
        return client.AsIChatClient(settings.Model);
    }

    private async Task<IChatClient> CreateCopilotClientAsync(CopilotSettings settings, CancellationToken ct)
    {
        return await CopilotChatClientAdapter.CreateAsync(settings, _logger, ct);
    }

    // ── IChatClient delegation ──

    public Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
        => _inner.GetResponseAsync(messages, options, cancellationToken);

    public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
        => _inner.GetStreamingResponseAsync(messages, options, cancellationToken);

    public object? GetService(Type serviceType, object? serviceKey = null)
        => _inner.GetService(serviceType, serviceKey);

    public void Dispose()
    {
        if (_inner is IDisposable disposable)
            disposable.Dispose();
    }
}
