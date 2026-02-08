// elbruno.Doc2Code — HTTP client wrapper for the SettingsService REST API.
namespace elbruno.Doc2Code.Web.Services;

using System.Net.Http.Json;
using elbruno.Doc2Code.Core.DTOs;
using elbruno.Doc2Code.Core.Models;

/// <summary>
/// Provides typed methods for calling the SettingsService endpoints.
/// </summary>
public sealed class SettingsApiClient
{
    private readonly HttpClient _http;

    public SettingsApiClient(HttpClient http)
    {
        _http = http;
    }

    /// <summary>Fetches the current configuration from the SettingsService.</summary>
    public async Task<ConfigurationDto> FetchCurrentAsync(CancellationToken ct = default)
    {
        return await _http.GetFromJsonAsync<ConfigurationDto>("/api/settings", ct)
               ?? new ConfigurationDto();
    }

    /// <summary>Replaces the entire configuration via PUT.</summary>
    public async Task ReplaceAsync(ConfigurationDto dto, CancellationToken ct = default)
    {
        await _http.PutAsJsonAsync("/api/settings", dto, ct);
    }

    /// <summary>Applies individual tool toggle changes via PATCH.</summary>
    public async Task ApplyToolToggleAsync(Dictionary<string, bool> toolUpdates, CancellationToken ct = default)
    {
        await _http.PatchAsJsonAsync("/api/settings/tools", toolUpdates, ct);
    }

    /// <summary>Retrieves the list of suggested models.</summary>
    public async Task<List<ModelSuggestion>> GetModelRecommendationsAsync(CancellationToken ct = default)
    {
        return await _http.GetFromJsonAsync<List<ModelSuggestion>>("/api/settings/models/suggested", ct)
               ?? [];
    }
}
