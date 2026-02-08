// elbruno.Doc2Code — JSON-file persistence for Doc2CodeConfig.
namespace elbruno.Doc2Code.SettingsService.Services;

using System.Text.Json;
using System.Text.Json.Nodes;
using elbruno.Doc2Code.Core.Abstractions;
using elbruno.Doc2Code.Core.Models;

/// <summary>
/// Persists <see cref="Doc2CodeConfig"/> to a local JSON file.
/// Uses <see cref="SemaphoreSlim"/> for thread-safe read/write.
/// </summary>
public sealed class SettingsStore : ISettingsStore
{
    private readonly string _filePath;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private Doc2CodeConfig? _cached;

    private static readonly JsonSerializerOptions s_jsonOpts = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public SettingsStore(string dataDirectory)
    {
        Directory.CreateDirectory(dataDirectory);
        _filePath = Path.Combine(dataDirectory, "settings.json");
    }

    public async Task<Doc2CodeConfig> GetAsync(CancellationToken ct = default)
    {
        await _lock.WaitAsync(ct);
        try
        {
            if (_cached is not null) return _cached;

            if (File.Exists(_filePath))
            {
                var json = await File.ReadAllTextAsync(_filePath, ct);
                _cached = JsonSerializer.Deserialize<Doc2CodeConfig>(json, s_jsonOpts) ?? new Doc2CodeConfig();
                MigrateLegacyFields(json, _cached);
            }
            else
            {
                _cached = new Doc2CodeConfig();
                await PersistAsync(_cached, ct);
            }

            return _cached;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task SaveAsync(Doc2CodeConfig config, CancellationToken ct = default)
    {
        await _lock.WaitAsync(ct);
        try
        {
            _cached = config;
            await PersistAsync(config, ct);
        }
        finally
        {
            _lock.Release();
        }
    }

    private async Task PersistAsync(Doc2CodeConfig config, CancellationToken ct)
    {
        var json = JsonSerializer.Serialize(config, s_jsonOpts);
        await File.WriteAllTextAsync(_filePath, json, ct);
    }

    /// <summary>
    /// If the JSON has legacy <c>llmEndpoint</c>/<c>preferredModel</c> top-level fields
    /// but no <c>ollama</c> object, migrate those values into <see cref="Doc2CodeConfig.Ollama"/>.
    /// </summary>
    private static void MigrateLegacyFields(string json, Doc2CodeConfig config)
    {
        try
        {
            var node = JsonNode.Parse(json);
            if (node is null) return;

            var hasOllama = node["ollama"] is not null;
            if (hasOllama) return;

            var legacyEndpoint = node["llmEndpoint"]?.GetValue<string>();
            var legacyModel = node["preferredModel"]?.GetValue<string>();

            if (!string.IsNullOrEmpty(legacyEndpoint))
                config.Ollama.Endpoint = legacyEndpoint;

            if (!string.IsNullOrEmpty(legacyModel))
                config.Ollama.Model = legacyModel;
        }
        catch
        {
            // If migration fails, leave defaults.
        }
    }
}
