// elbruno.Doc2Code — typed HTTP client that calls the SettingsService for config.
namespace elbruno.Doc2Code.ApiService.Services;

using System.Net.Http.Json;
using elbruno.Doc2Code.Core.Abstractions;
using elbruno.Doc2Code.Core.DTOs;
using elbruno.Doc2Code.Core.Models;

/// <summary>
/// Read-only settings provider that calls the SettingsService via HTTP.
/// Caches locally with a 30-second TTL.
/// </summary>
public sealed class RemoteSettingsClient : ISettingsStore
{
    private readonly HttpClient _http;
    private Doc2CodeConfig? _cache;
    private DateTime _cacheExpiry = DateTime.MinValue;
    private static readonly TimeSpan CacheTtl = TimeSpan.FromSeconds(30);

    public RemoteSettingsClient(HttpClient http)
    {
        _http = http;
    }

    public async Task<Doc2CodeConfig> GetAsync(CancellationToken ct = default)
    {
        if (_cache is not null && DateTime.UtcNow < _cacheExpiry)
            return _cache;

        var dto = await _http.GetFromJsonAsync<ConfigurationDto>("/api/settings", ct)
                  ?? new ConfigurationDto();

        _cache = new Doc2CodeConfig
        {
            LlmProvider = dto.LlmProvider,
            Ollama = dto.Ollama,
            FoundryLocal = dto.FoundryLocal,
            AzureAIInference = dto.AzureAIInference,
            AgentProfiles = dto.AgentProfiles,
            SourceControl = new SourceControlConfig
            {
                Token = dto.GitHubToken,
                OwnerOrOrg = dto.GitHubOwner
            },
            EnabledTools = dto.EnabledTools
        };
        _cacheExpiry = DateTime.UtcNow + CacheTtl;
        return _cache;
    }

    public async Task SaveAsync(Doc2CodeConfig config, CancellationToken ct = default)
    {
        var dto = new ConfigurationDto
        {
            LlmProvider = config.LlmProvider,
            Ollama = config.Ollama,
            FoundryLocal = config.FoundryLocal,
            AzureAIInference = config.AzureAIInference,
            LlmEndpoint = config.LlmEndpoint,
            PreferredModel = config.PreferredModel,
            AgentProfiles = config.AgentProfiles,
            GitHubToken = config.SourceControl.Token,
            GitHubOwner = config.SourceControl.OwnerOrOrg,
            EnabledTools = config.EnabledTools
        };
        await _http.PutAsJsonAsync("/api/settings", dto, ct);
        _cache = config;
        _cacheExpiry = DateTime.UtcNow + CacheTtl;
    }
}
