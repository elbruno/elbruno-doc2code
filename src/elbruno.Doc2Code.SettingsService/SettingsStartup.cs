// elbruno.Doc2Code — DI registration and route mapping for the SettingsService.
namespace elbruno.Doc2Code.SettingsService;

using elbruno.Doc2Code.Core.DTOs;
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
            var dto = new ConfigurationDto
            {
                LlmProvider = config.LlmProvider,
                Ollama = config.Ollama,
                FoundryLocal = config.FoundryLocal,
                AzureAIInference = config.AzureAIInference,
                Copilot = config.Copilot,
                LlmEndpoint = config.LlmEndpoint,
                PreferredModel = config.PreferredModel,
#pragma warning disable CS0618
                AgentProfiles = config.AgentProfiles,
#pragma warning restore CS0618
                AgentDefinitions = config.AgentDefinitions,
                Pipelines = config.Pipelines,
                GitHubToken = config.SourceControl.Token,
                GitHubOwner = config.SourceControl.OwnerOrOrg,
                EnabledTools = config.EnabledTools,
                SuggestedModels = config.SuggestedModels,
                AgentDefaults = BuildAgentDefaults()
            };
            return Results.Ok(dto);
        });

        settingsGroup.MapPut("/", async (ConfigurationDto dto, ISettingsStore store, CancellationToken ct) =>
        {
            var config = new Doc2CodeConfig
            {
                LlmProvider = dto.LlmProvider,
                Ollama = dto.Ollama,
                FoundryLocal = dto.FoundryLocal,
                AzureAIInference = dto.AzureAIInference,
                Copilot = dto.Copilot,
#pragma warning disable CS0618
                AgentProfiles = dto.AgentProfiles,
#pragma warning restore CS0618
                AgentDefinitions = dto.AgentDefinitions,
                Pipelines = dto.Pipelines,
                SourceControl = new SourceControlConfig
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

        // ── Agent Definition CRUD ──────────────────────────────────────

        settingsGroup.MapGet("/agents", async (ISettingsStore store, CancellationToken ct) =>
        {
            var config = await store.GetAsync(ct);
            return Results.Ok(config.AgentDefinitions);
        });

        settingsGroup.MapPost("/agents", async (AgentDefinition agent, ISettingsStore store, CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(agent.AgentKey))
                return Results.BadRequest("AgentKey is required.");
            if (string.IsNullOrWhiteSpace(agent.SystemPrompt))
                return Results.BadRequest("SystemPrompt is required for custom agents.");

            var config = await store.GetAsync(ct);
            if (config.AgentDefinitions.Any(a => a.AgentKey.Equals(agent.AgentKey, StringComparison.OrdinalIgnoreCase)))
                return Results.Conflict($"Agent with key '{agent.AgentKey}' already exists.");

            config.AgentDefinitions.Add(agent);
            await store.SaveAsync(config, ct);
            return Results.Created($"/api/settings/agents/{agent.AgentKey}", agent);
        });

        settingsGroup.MapPut("/agents/{key}", async (string key, AgentDefinition agent, ISettingsStore store, CancellationToken ct) =>
        {
            var config = await store.GetAsync(ct);
            var existing = config.AgentDefinitions.FirstOrDefault(a => a.AgentKey.Equals(key, StringComparison.OrdinalIgnoreCase));
            if (existing is null)
                return Results.NotFound($"Agent '{key}' not found.");

            if (existing.IsBuiltIn && !existing.AgentKey.Equals(agent.AgentKey, StringComparison.OrdinalIgnoreCase))
                return Results.BadRequest("Cannot change the AgentKey of a built-in agent.");

            var idx = config.AgentDefinitions.IndexOf(existing);
            config.AgentDefinitions[idx] = agent;
            await store.SaveAsync(config, ct);
            return Results.Ok(agent);
        });

        settingsGroup.MapDelete("/agents/{key}", async (string key, ISettingsStore store, CancellationToken ct) =>
        {
            var config = await store.GetAsync(ct);
            var existing = config.AgentDefinitions.FirstOrDefault(a => a.AgentKey.Equals(key, StringComparison.OrdinalIgnoreCase));
            if (existing is null)
                return Results.NotFound($"Agent '{key}' not found.");
            if (existing.IsBuiltIn)
                return Results.BadRequest("Cannot delete a built-in agent.");

            config.AgentDefinitions.Remove(existing);
            await store.SaveAsync(config, ct);
            return Results.Ok(new { deleted = true });
        });

        // ── Pipeline CRUD ──────────────────────────────────────────────

        settingsGroup.MapGet("/pipelines", async (ISettingsStore store, CancellationToken ct) =>
        {
            var config = await store.GetAsync(ct);
            return Results.Ok(config.Pipelines);
        });

        settingsGroup.MapPost("/pipelines", async (PipelineDefinition pipeline, ISettingsStore store, CancellationToken ct) =>
        {
            var config = await store.GetAsync(ct);
            if (config.Pipelines.Any(p => p.Id == pipeline.Id))
                return Results.Conflict($"Pipeline with ID '{pipeline.Id}' already exists.");

            config.Pipelines.Add(pipeline);
            await store.SaveAsync(config, ct);
            return Results.Created($"/api/settings/pipelines/{pipeline.Id}", pipeline);
        });

        settingsGroup.MapPut("/pipelines/{id}", async (string id, PipelineDefinition pipeline, ISettingsStore store, CancellationToken ct) =>
        {
            var config = await store.GetAsync(ct);
            var existing = config.Pipelines.FirstOrDefault(p => p.Id == id);
            if (existing is null)
                return Results.NotFound($"Pipeline '{id}' not found.");

            pipeline.Version = existing.Version + 1;
            pipeline.UpdatedAtUtc = DateTime.UtcNow;
            var idx = config.Pipelines.IndexOf(existing);
            config.Pipelines[idx] = pipeline;
            await store.SaveAsync(config, ct);
            return Results.Ok(pipeline);
        });

        settingsGroup.MapDelete("/pipelines/{id}", async (string id, ISettingsStore store, CancellationToken ct) =>
        {
            var config = await store.GetAsync(ct);
            var existing = config.Pipelines.FirstOrDefault(p => p.Id == id);
            if (existing is null)
                return Results.NotFound($"Pipeline '{id}' not found.");
            if (existing.IsActive)
                return Results.BadRequest("Cannot delete the active pipeline.");

            config.Pipelines.Remove(existing);
            await store.SaveAsync(config, ct);
            return Results.Ok(new { deleted = true });
        });

        settingsGroup.MapPost("/pipelines/{id}/activate", async (string id, ISettingsStore store, CancellationToken ct) =>
        {
            var config = await store.GetAsync(ct);
            var target = config.Pipelines.FirstOrDefault(p => p.Id == id);
            if (target is null)
                return Results.NotFound($"Pipeline '{id}' not found.");

            foreach (var p in config.Pipelines)
                p.IsActive = false;
            target.IsActive = true;

            await store.SaveAsync(config, ct);
            return Results.Ok(new { activated = true });
        });

        settingsGroup.MapPost("/pipelines/{id}/clone", async (string id, ISettingsStore store, CancellationToken ct) =>
        {
            var config = await store.GetAsync(ct);
            var source = config.Pipelines.FirstOrDefault(p => p.Id == id);
            if (source is null)
                return Results.NotFound($"Pipeline '{id}' not found.");

            var json = System.Text.Json.JsonSerializer.Serialize(source);
            var clone = System.Text.Json.JsonSerializer.Deserialize<PipelineDefinition>(json)!;
            clone.Id = Guid.NewGuid().ToString();
            clone.Name = $"{source.Name} (Copy)";
            clone.IsDefault = false;
            clone.IsActive = false;
            clone.Version = 1;
            clone.CreatedAtUtc = DateTime.UtcNow;
            clone.UpdatedAtUtc = DateTime.UtcNow;

            config.Pipelines.Add(clone);
            await store.SaveAsync(config, ct);
            return Results.Created($"/api/settings/pipelines/{clone.Id}", clone);
        });

        // ── Export / Import ────────────────────────────────────────────

        settingsGroup.MapGet("/export", async (ISettingsStore store, CancellationToken ct) =>
        {
            var config = await store.GetAsync(ct);
            var bundle = new SettingsExportBundle
            {
                ExportedAt = DateTime.UtcNow,
                AgentDefinitions = config.AgentDefinitions,
                Pipelines = config.Pipelines,
                EnabledTools = config.EnabledTools
            };
            return Results.Ok(bundle);
        });

        settingsGroup.MapPost("/import", async (SettingsExportBundle bundle, ISettingsStore store, CancellationToken ct) =>
        {
            var config = await store.GetAsync(ct);

            // Merge agent definitions — don't overwrite built-in agents
            foreach (var agent in bundle.AgentDefinitions)
            {
                var existing = config.AgentDefinitions.FirstOrDefault(a =>
                    a.AgentKey.Equals(agent.AgentKey, StringComparison.OrdinalIgnoreCase));
                if (existing is null)
                    config.AgentDefinitions.Add(agent);
                else if (!existing.IsBuiltIn)
                {
                    var idx = config.AgentDefinitions.IndexOf(existing);
                    config.AgentDefinitions[idx] = agent;
                }
            }

            // Merge pipelines
            foreach (var pipeline in bundle.Pipelines)
            {
                var existing = config.Pipelines.FirstOrDefault(p => p.Id == pipeline.Id);
                if (existing is null)
                {
                    pipeline.IsActive = false; // Don't auto-activate imported pipelines
                    config.Pipelines.Add(pipeline);
                }
                else
                {
                    var idx = config.Pipelines.IndexOf(existing);
                    pipeline.IsActive = existing.IsActive; // Preserve active state
                    config.Pipelines[idx] = pipeline;
                }
            }

            // Merge tool toggles
            foreach (var (key, enabled) in bundle.EnabledTools)
                config.EnabledTools[key] = enabled;

            await store.SaveAsync(config, ct);
            return Results.Ok(new { imported = true });
        });

        settingsGroup.MapGet("/pipelines/{id}/export", async (string id, ISettingsStore store, CancellationToken ct) =>
        {
            var config = await store.GetAsync(ct);
            var pipeline = config.Pipelines.FirstOrDefault(p => p.Id == id);
            if (pipeline is null)
                return Results.NotFound($"Pipeline '{id}' not found.");

            // Include only agent definitions referenced by the pipeline
            var referencedKeys = pipeline.Steps.Select(s => s.AgentKey).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var agents = config.AgentDefinitions.Where(a => referencedKeys.Contains(a.AgentKey)).ToList();

            var bundle = new SettingsExportBundle
            {
                ExportedAt = DateTime.UtcNow,
                AgentDefinitions = agents,
                Pipelines = [pipeline]
            };
            return Results.Ok(bundle);
        });

        settingsGroup.MapPost("/pipelines/import", async (SettingsExportBundle bundle, ISettingsStore store, CancellationToken ct) =>
        {
            var config = await store.GetAsync(ct);

            // Import agent definitions
            foreach (var agent in bundle.AgentDefinitions)
            {
                if (!config.AgentDefinitions.Any(a => a.AgentKey.Equals(agent.AgentKey, StringComparison.OrdinalIgnoreCase)))
                    config.AgentDefinitions.Add(agent);
            }

            // Import pipelines
            foreach (var pipeline in bundle.Pipelines)
            {
                pipeline.IsActive = false;
                if (!config.Pipelines.Any(p => p.Id == pipeline.Id))
                    config.Pipelines.Add(pipeline);
            }

            await store.SaveAsync(config, ct);
            return Results.Ok(new { imported = true });
        });
    }
}
