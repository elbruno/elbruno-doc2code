// elbruno.Doc2Code — dynamic LLM agent driven by an AgentDefinition.
namespace elbruno.Doc2Code.Agents;

using System.Text.Json;
using System.Text.RegularExpressions;
using elbruno.Doc2Code.Core.Abstractions;
using elbruno.Doc2Code.Core.Models;
using elbruno.Doc2Code.Tools;
using Microsoft.Extensions.AI;

/// <summary>
/// A dynamic agent that uses an <see cref="AgentDefinition"/> to call the LLM
/// with interpolated prompts and stores the result in the <see cref="PipelineDataBag"/>.
/// </summary>
public sealed class DynamicLlmAgent : IDynamicAgent
{
    private readonly AgentDefinition _definition;
    private readonly IChatClient _chat;
    private readonly ToolRegistry? _toolRegistry;
    private Dictionary<string, bool>? _enabledToolToggles;

    private static readonly JsonSerializerOptions s_jsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public DynamicLlmAgent(AgentDefinition definition, IChatClient chat, ToolRegistry? toolRegistry = null)
    {
        _definition = definition;
        _chat = chat;
        _toolRegistry = toolRegistry;
    }

    public string AgentKey => _definition.AgentKey;
    public string DisplayName => _definition.DisplayName;

    /// <summary>Sets tool toggles for filtering available tools.</summary>
    public void SetToolToggles(Dictionary<string, bool>? toggles) => _enabledToolToggles = toggles;

    public async Task<PipelineDataBag> RunAsync(PipelineDataBag input, IProgress<AgentLogEntry> log, CancellationToken ct)
    {
        log.Report(new AgentLogEntry
        {
            SourceAgent = DisplayName,
            Text = $"Starting {DisplayName}…",
            Severity = LogSeverity.Informational
        });

        // Interpolate user prompt template with bag values
        var userMessage = InterpolateTemplate(_definition.UserPromptTemplate, input);

        var conversation = new List<ChatMessage>
        {
            new(ChatRole.System, _definition.SystemPrompt),
            new(ChatRole.User, userMessage)
        };

        // Build chat options with tools if available
        ChatOptions? options = null;
        if (_toolRegistry is not null && _definition.SupportsTools && _enabledToolToggles is not null)
        {
            var tools = _toolRegistry.ResolveEnabledTools(_enabledToolToggles);
            if (tools.Count > 0)
                options = new ChatOptions { Tools = tools };
        }

        var response = await _chat.GetResponseAsync(conversation, options, ct);
        var raw = response.Text ?? "";

        // Strip markdown code fences
        var cleaned = StripCodeFences(raw);

        log.Report(new AgentLogEntry
        {
            SourceAgent = DisplayName,
            Text = $"LLM responded ({raw.Length} chars). Storing under '{_definition.OutputKey}'.",
            Severity = LogSeverity.Informational
        });

        // Parse as JsonElement and store in the bag
        try
        {
            var element = JsonSerializer.Deserialize<JsonElement>(cleaned, s_jsonOpts);
            input.Set(_definition.OutputKey, element);
        }
        catch (JsonException)
        {
            // Store as raw text if not valid JSON
            input.Set(_definition.OutputKey, cleaned);
        }

        log.Report(new AgentLogEntry
        {
            SourceAgent = DisplayName,
            Text = $"{DisplayName} completed.",
            Severity = LogSeverity.Informational
        });

        return input;
    }

    /// <summary>Replaces <c>{key}</c> placeholders in the template with bag values.</summary>
    private static string InterpolateTemplate(string template, PipelineDataBag bag)
    {
        return Regex.Replace(template, @"\{(\w+)\}", match =>
        {
            var key = match.Groups[1].Value;
            if (bag.ContainsKey(key))
            {
                try
                {
                    var raw = bag.GetRaw(key);
                    return raw.ValueKind == JsonValueKind.String
                        ? raw.GetString() ?? match.Value
                        : raw.GetRawText();
                }
                catch
                {
                    return match.Value;
                }
            }
            return match.Value;
        });
    }

    private static string StripCodeFences(string raw)
    {
        var trimmed = raw.Trim();
        if (trimmed.StartsWith("```"))
        {
            var firstNewline = trimmed.IndexOf('\n');
            if (firstNewline >= 0)
                trimmed = trimmed[(firstNewline + 1)..];
        }
        if (trimmed.EndsWith("```"))
            trimmed = trimmed[..^3];
        return trimmed.Trim();
    }
}
