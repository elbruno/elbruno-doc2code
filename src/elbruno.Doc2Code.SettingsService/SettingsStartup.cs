// elbruno.Doc2Code — DI registration and route mapping for the SettingsService.
namespace elbruno.Doc2Code.SettingsService;

using elbruno.Doc2Code.Core.Prompts;
using elbruno.Doc2Code.Core.Abstractions;
using elbruno.Doc2Code.Core.Models;
using elbruno.Doc2Code.SettingsService.Services;

public static class SettingsStartup
{
    /// <summary>Returns default agent info (system prompts) keyed by agent name.</summary>
    private static Dictionary<string, AgentDefaultInfo> BuildAgentDefaults() => new()
    {
        ["Analyst"] = new() { AgentKey = "Analyst", DefaultSystemPrompt = AgentInstructions.ForAnalyst },
        ["Architect"] = new() { AgentKey = "Architect", DefaultSystemPrompt = AgentInstructions.ForArchitect },
        ["Developer"] = new() { AgentKey = "Developer", DefaultSystemPrompt = AgentInstructions.ForDeveloper },
        ["Reviewer"] = new() { AgentKey = "Reviewer", DefaultSystemPrompt = AgentInstructions.ForReviewer },
        ["Testing"] = new() { AgentKey = "Testing", DefaultSystemPrompt = AgentInstructions.ForTesting },
        ["Documentation"] = new() { AgentKey = "Documentation", DefaultSystemPrompt = AgentInstructions.ForDocumentation },
    };

    public static void MapSettingsRoutes(this WebApplication app)
    {
        app.MapDefaultEndpoints();

        var settingsGroup = app.MapGroup("/api/settings");

        settingsGroup.MapGet("/", async (ISettingsStore store, CancellationToken ct) =>
        {
            var config = await store.GetAsync(ct);
            var dto = new elbruno.Doc2Code.Core.DTOs.ConfigurationDto
            {
                LlmProvider = config.LlmProvider,
                Ollama = config.Ollama,
                FoundryLocal = config.FoundryLocal,
                AzureAIInference = config.AzureAIInference,
                Copilot = config.Copilot,
                LlmEndpoint = config.LlmEndpoint,
                PreferredModel = config.PreferredModel,
                AgentProfiles = config.AgentProfiles,
                GitHubToken = config.SourceControl.Token,
                GitHubOwner = config.SourceControl.OwnerOrOrg,
                EnabledTools = config.EnabledTools,
                SuggestedModels = config.SuggestedModels,
                AgentDefaults = BuildAgentDefaults()
            };
            return Results.Ok(dto);
        });

        settingsGroup.MapPut("/", async (elbruno.Doc2Code.Core.DTOs.ConfigurationDto dto, ISettingsStore store, CancellationToken ct) =>
        {
            var config = new elbruno.Doc2Code.Core.Models.Doc2CodeConfig
            {
                LlmProvider = dto.LlmProvider,
                Ollama = dto.Ollama,
                FoundryLocal = dto.FoundryLocal,
                AzureAIInference = dto.AzureAIInference,
                Copilot = dto.Copilot,
                AgentProfiles = dto.AgentProfiles,
                SourceControl = new elbruno.Doc2Code.Core.Models.SourceControlConfig
                {
                    Token = dto.GitHubToken,
                    OwnerOrOrg = dto.GitHubOwner
                },
                EnabledTools = dto.EnabledTools
            };
            await store.SaveAsync(config, ct);
            return Results.Ok(new { saved = true });
        });

        settingsGroup.MapPatch("/tools", async (Dictionary<string, bool> toolUpdates, ISettingsStore store, CancellationToken ct) =>
        {
            var config = await store.GetAsync(ct);
            foreach (var (key, enabled) in toolUpdates)
            {
                config.EnabledTools[key] = enabled;
            }
            await store.SaveAsync(config, ct);
            return Results.Ok(config.EnabledTools);
        });

        settingsGroup.MapGet("/models/suggested", async (ISettingsStore store, CancellationToken ct) =>
        {
            var config = await store.GetAsync(ct);
            return Results.Ok(config.SuggestedModels);
        });
    }
}
