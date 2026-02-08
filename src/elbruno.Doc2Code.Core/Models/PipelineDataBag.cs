// elbruno.Doc2Code — uniform I/O contract for all agents in a dynamic pipeline.
namespace elbruno.Doc2Code.Core.Models;

using System.Text.Json;

/// <summary>
/// A typed dictionary that serves as the shared data bus for all agents in a pipeline run.
/// Each agent reads its inputs and writes its output to this bag using string keys.
/// </summary>
public sealed class PipelineDataBag
{
    private static readonly JsonSerializerOptions s_opts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    private readonly Dictionary<string, JsonElement> _data = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>The original requirements document seeded at pipeline start.</summary>
    public RequirementsDocument? OriginalSpec { get; set; }

    /// <summary>Stores a value in the bag under the given key.</summary>
    public void Set<T>(string key, T value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        var json = JsonSerializer.Serialize(value, s_opts);
        _data[key] = JsonSerializer.Deserialize<JsonElement>(json, s_opts);
    }

    /// <summary>Retrieves a value from the bag, deserializing to the requested type.</summary>
    public T Get<T>(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        if (!_data.TryGetValue(key, out var element))
            throw new KeyNotFoundException($"Key '{key}' not found in pipeline data bag.");

        return JsonSerializer.Deserialize<T>(element.GetRawText(), s_opts)
            ?? throw new InvalidOperationException($"Failed to deserialize key '{key}' to {typeof(T).Name}.");
    }

    /// <summary>Tries to retrieve a value from the bag.</summary>
    public bool TryGet<T>(string key, out T? value)
    {
        value = default;
        if (string.IsNullOrWhiteSpace(key) || !_data.TryGetValue(key, out var element))
            return false;

        try
        {
            value = JsonSerializer.Deserialize<T>(element.GetRawText(), s_opts);
            return value is not null;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>Returns whether the bag contains the given key.</summary>
    public bool ContainsKey(string key) => _data.ContainsKey(key);

    /// <summary>All keys currently stored in the bag.</summary>
    public IEnumerable<string> Keys => _data.Keys;

    /// <summary>Retrieves the raw <see cref="JsonElement"/> for a key.</summary>
    public JsonElement GetRaw(string key) => _data[key];
}
