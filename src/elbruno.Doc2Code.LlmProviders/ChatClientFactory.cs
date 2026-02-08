// elbruno.Doc2Code — factory that creates IChatClient instances from current settings.
namespace elbruno.Doc2Code.LlmProviders.Services;

using elbruno.Doc2Code.Core.Abstractions;
using Microsoft.Extensions.AI;

/// <summary>
/// Creates <see cref="IChatClient"/> instances using the latest settings
/// from <see cref="ISettingsStore"/>, ensuring runtime config changes take effect.
/// </summary>
public sealed class ChatClientFactory(ISettingsStore settingsStore)
{
    public async Task<IChatClient> CreateAsync(CancellationToken ct = default)
    {
        var config = await settingsStore.GetAsync(ct);

        if (!Uri.TryCreate(config.LlmEndpoint, UriKind.Absolute, out var endpoint))
            throw new InvalidOperationException(
                $"LlmEndpoint '{config.LlmEndpoint}' is not a valid absolute URI.");

        return new OllamaSharp.OllamaApiClient(endpoint, config.PreferredModel);
    }
}
