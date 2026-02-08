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

    // ── Agent Definition CRUD ──────────────────────────────────────

    /// <summary>Lists all agent definitions.</summary>
    public async Task<List<AgentDefinition>> GetAgentDefinitionsAsync(CancellationToken ct = default)
    {
        return await _http.GetFromJsonAsync<List<AgentDefinition>>("/api/settings/agents", ct) ?? [];
    }

    /// <summary>Creates a custom agent definition.</summary>
    public async Task<AgentDefinition?> CreateAgentAsync(AgentDefinition agent, CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync("/api/settings/agents", agent, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<AgentDefinition>(ct);
    }

    /// <summary>Updates an agent definition.</summary>
    public async Task UpdateAgentAsync(string key, AgentDefinition agent, CancellationToken ct = default)
    {
        var response = await _http.PutAsJsonAsync($"/api/settings/agents/{key}", agent, ct);
        response.EnsureSuccessStatusCode();
    }

    /// <summary>Deletes a custom agent definition.</summary>
    public async Task DeleteAgentAsync(string key, CancellationToken ct = default)
    {
        var response = await _http.DeleteAsync($"/api/settings/agents/{key}", ct);
        response.EnsureSuccessStatusCode();
    }

    // ── Pipeline CRUD ──────────────────────────────────────────────

    /// <summary>Lists all pipeline definitions.</summary>
    public async Task<List<PipelineDefinition>> GetPipelinesAsync(CancellationToken ct = default)
    {
        return await _http.GetFromJsonAsync<List<PipelineDefinition>>("/api/settings/pipelines", ct) ?? [];
    }

    /// <summary>Creates a new pipeline.</summary>
    public async Task<PipelineDefinition?> CreatePipelineAsync(PipelineDefinition pipeline, CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync("/api/settings/pipelines", pipeline, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<PipelineDefinition>(ct);
    }

    /// <summary>Updates an existing pipeline.</summary>
    public async Task UpdatePipelineAsync(string id, PipelineDefinition pipeline, CancellationToken ct = default)
    {
        var response = await _http.PutAsJsonAsync($"/api/settings/pipelines/{id}", pipeline, ct);
        response.EnsureSuccessStatusCode();
    }

    /// <summary>Deletes a pipeline.</summary>
    public async Task DeletePipelineAsync(string id, CancellationToken ct = default)
    {
        var response = await _http.DeleteAsync($"/api/settings/pipelines/{id}", ct);
        response.EnsureSuccessStatusCode();
    }

    /// <summary>Activates a pipeline.</summary>
    public async Task ActivatePipelineAsync(string id, CancellationToken ct = default)
    {
        var response = await _http.PostAsync($"/api/settings/pipelines/{id}/activate", null, ct);
        response.EnsureSuccessStatusCode();
    }

    /// <summary>Clones a pipeline.</summary>
    public async Task<PipelineDefinition?> ClonePipelineAsync(string id, CancellationToken ct = default)
    {
        var response = await _http.PostAsync($"/api/settings/pipelines/{id}/clone", null, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<PipelineDefinition>(ct);
    }

    // ── Export / Import ────────────────────────────────────────────

    /// <summary>Exports all settings (excludes secrets).</summary>
    public async Task<SettingsExportBundle?> ExportAsync(CancellationToken ct = default)
    {
        return await _http.GetFromJsonAsync<SettingsExportBundle>("/api/settings/export", ct);
    }

    /// <summary>Imports a settings bundle.</summary>
    public async Task ImportAsync(SettingsExportBundle bundle, CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync("/api/settings/import", bundle, ct);
        response.EnsureSuccessStatusCode();
    }

    /// <summary>Exports a single pipeline with its referenced agents.</summary>
    public async Task<SettingsExportBundle?> ExportPipelineAsync(string id, CancellationToken ct = default)
    {
        return await _http.GetFromJsonAsync<SettingsExportBundle>($"/api/settings/pipelines/{id}/export", ct);
    }

    /// <summary>Imports a single pipeline bundle.</summary>
    public async Task ImportPipelineAsync(SettingsExportBundle bundle, CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync("/api/settings/pipelines/import", bundle, ct);
        response.EnsureSuccessStatusCode();
    }
}
