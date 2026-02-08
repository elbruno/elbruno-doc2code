// elbruno.Doc2Code — contract for persisting and retrieving application configuration.
namespace elbruno.Doc2Code.Core.Abstractions;

using elbruno.Doc2Code.Core.Models;

/// <summary>Provides async read/write access to the persisted <see cref="Doc2CodeConfig"/>.</summary>
public interface ISettingsStore
{
    Task<Doc2CodeConfig> GetAsync(CancellationToken ct = default);
    Task SaveAsync(Doc2CodeConfig config, CancellationToken ct = default);
}
