// elbruno.Doc2Code — shared base for all LLM-backed agents in the pipeline.
namespace elbruno.Doc2Code.Agents;

using System.Diagnostics;
using System.Text;
using System.Text.Json;
using elbruno.Doc2Code.Tools;
using elbruno.Doc2Code.Core.Abstractions;
using elbruno.Doc2Code.Core.Models;
using elbruno.Doc2Code.ServiceDefaults;
using Microsoft.Extensions.AI;

/// <summary>
/// Provides the common plumbing every agent needs: calling the LLM,
/// parsing the JSON response, and emitting progress logs.
/// Subclasses only define their prompt and how to map JSON to their output type.
/// </summary>
public abstract class LlmAgentBase<TIn, TOut> : IAgent<TIn, TOut>
{
    private readonly IChatClient _chat;
    private readonly ToolRegistry? _toolRegistry;
    private static readonly JsonSerializerOptions s_jsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    private const int MaxToolCallIterations = 5;
    private const int MaxJsonRetries = 2;
    private static readonly TimeSpan StreamingThrottleInterval = TimeSpan.FromMilliseconds(200);

    protected LlmAgentBase(IChatClient chat, ToolRegistry? toolRegistry = null)
    {
        _chat = chat;
        _toolRegistry = toolRegistry;
    }

    /// <inheritdoc />
    public abstract string DisplayName { get; }

    /// <summary>The system instruction sent to the LLM.</summary>
    protected abstract string SystemInstruction { get; }

    /// <summary>Build the user-message content from the typed input.</summary>
    protected abstract string ComposeUserMessage(TIn payload);

    /// <summary>Deserialise the LLM's JSON output into the agent's output type.</summary>
    protected abstract TOut ParseResponse(string json);

    /// <summary>
    /// Indicates whether this agent supports tool calls.
    /// Override to <c>false</c> if tools are inappropriate for the agent's domain.
    /// </summary>
    public virtual bool SupportsTools => true;

    /// <summary>
    /// Sets the current tool toggles for filtering which tools are active during a run.
    /// Called by the orchestrator before each pipeline execution.
    /// </summary>
    public Dictionary<string, bool>? EnabledToolToggles { get; set; }

    /// <inheritdoc />
    public async Task<TOut> RunAsync(
        TIn payload, IProgress<AgentLogEntry> log, CancellationToken ct = default)
    {
        Emit(log, $"Starting {DisplayName}…");

        using var span = Doc2CodeObservability.StartAgentSpan(DisplayName, "run");

        var userContent = ComposeUserMessage(payload);

        // Emit prompt as expandable log entry
        var promptText = $"[System Instruction]\n{SystemInstruction}\n\n[User Message]\n{userContent}";
        log.Report(new AgentLogEntry
        {
            SourceAgent = DisplayName,
            Text = $"Prompt sent ({promptText.Length} chars)",
            Severity = LogSeverity.Prompt,
            ExpandableContent = promptText
        });

        Emit(log, $"Prompt prepared ({userContent.Length} chars). Calling LLM…");

        var conversation = new List<ChatMessage>
        {
            new(ChatRole.System, SystemInstruction),
            new(ChatRole.User, userContent)
        };

        // Build chat options with tools if available
        ChatOptions? options = null;
        if (_toolRegistry is not null && SupportsTools && EnabledToolToggles is not null)
        {
            var tools = _toolRegistry.ResolveEnabledTools(EnabledToolToggles);
            if (tools.Count > 0)
            {
                options = new ChatOptions { Tools = tools };
                Emit(log, $"Tools enabled: {tools.Count} tool(s) available");
            }
        }

        var (response, accumulated) = await StreamResponseAsync(conversation, options, log, ct);

        // Handle tool-call loop
        var iterations = 0;
        while (iterations < MaxToolCallIterations && HasFunctionCalls(response))
        {
            iterations++;
            Emit(log, $"Processing tool calls (iteration {iterations})…");

            conversation.AddRange(response.Messages);

            foreach (var msg in response.Messages)
            {
                foreach (var call in msg.Contents.OfType<FunctionCallContent>())
                {
                    var tool = options?.Tools?.OfType<AIFunction>()
                        .FirstOrDefault(t => t.Name == call.Name);

                    object? result = null;
                    if (tool is not null)
                    {
                        var args = call.Arguments is not null
                            ? new AIFunctionArguments(call.Arguments)
                            : null;
                        result = await tool.InvokeAsync(args, ct);
                    }
                    else
                    {
                        result = $"Tool '{call.Name}' not found.";
                    }

                    conversation.Add(new ChatMessage(ChatRole.Tool,
                    [
                        new FunctionResultContent(call.CallId, result?.ToString() ?? "")
                    ]));
                }
            }

            (response, accumulated) = await StreamResponseAsync(conversation, options, log, ct);
        }

        var raw = accumulated;

        Emit(log, $"LLM responded ({raw.Length} chars). Parsing…");

        // Strip markdown code fences the model sometimes adds
        var cleaned = StripCodeFences(raw);

        try
        {
            var result = ParseResponse(cleaned);
            Emit(log, $"{DisplayName} completed successfully.");
            return result;
        }
        catch (JsonException ex)
        {
            // Phase 1: attempt JSON repair (no LLM call)
            var (repairedJson, appliedFixes) = JsonRepairHelper.TryRepair(cleaned);
            if (appliedFixes.Count > 0)
            {
                Emit(log, $"[Warning] JSON repair applied: {string.Join("; ", appliedFixes)}", LogSeverity.Caution);
                try
                {
                    var result = ParseResponse(repairedJson);
                    Emit(log, $"{DisplayName} completed successfully after JSON repair.");
                    return result;
                }
                catch (JsonException)
                {
                    // Repair wasn't enough — fall through to retry
                }
            }

            // Phase 2: LLM re-prompt retries
            conversation.Add(new ChatMessage(ChatRole.Assistant, raw));
            for (int attempt = 1; attempt <= MaxJsonRetries; attempt++)
            {
                Emit(log, $"[Warning] Retrying after JSON parse error (attempt {attempt})…", LogSeverity.Caution);

                conversation.Add(new ChatMessage(ChatRole.User,
                    $"Your previous response contained invalid JSON. Error: {ex.Message}. " +
                    "Please respond again with the complete valid JSON only, no explanations."));

                var (retryResponse, retryAccumulated) = await StreamResponseAsync(conversation, options, log, ct);
                var retryCleaned = StripCodeFences(retryAccumulated);

                // Try direct parse first
                try
                {
                    var result = ParseResponse(retryCleaned);
                    Emit(log, $"{DisplayName} completed successfully on retry {attempt}.");
                    return result;
                }
                catch (JsonException)
                {
                    // Try repair on retry output
                    var (retryRepaired, retryFixes) = JsonRepairHelper.TryRepair(retryCleaned);
                    if (retryFixes.Count > 0)
                    {
                        Emit(log, $"[Warning] JSON repair applied on retry {attempt}: {string.Join("; ", retryFixes)}", LogSeverity.Caution);
                        try
                        {
                            var result = ParseResponse(retryRepaired);
                            Emit(log, $"{DisplayName} completed successfully after retry {attempt} + repair.");
                            return result;
                        }
                        catch (JsonException)
                        {
                            // Continue to next retry
                        }
                    }

                    // Add the failed retry response to conversation for next attempt
                    conversation.Add(new ChatMessage(ChatRole.Assistant, retryAccumulated));
                }
            }

            // All attempts exhausted
            Emit(log, $"JSON parse error after {MaxJsonRetries} retries: {ex.Message}", LogSeverity.Failure);
            throw new JsonException(
                $"[{DisplayName}] JSON parse failed after repair + {MaxJsonRetries} retries " +
                $"(raw response: {raw.Length} chars): {ex.Message}", ex.Path, ex.LineNumber, ex.BytePositionInLine, ex);
        }
    }

    /// <summary>
    /// Streams the LLM response, emitting throttled streaming chunk log entries,
    /// and returns the final <see cref="ChatResponse"/> along with the accumulated text.
    /// </summary>
    private async Task<(ChatResponse Response, string AccumulatedText)> StreamResponseAsync(
        IList<ChatMessage> conversation, ChatOptions? options,
        IProgress<AgentLogEntry> log, CancellationToken ct)
    {
        var sb = new StringBuilder();
        var sw = Stopwatch.StartNew();

        await foreach (var update in _chat.GetStreamingResponseAsync(conversation, options, cancellationToken: ct))
        {
            if (update.Text is { Length: > 0 })
            {
                sb.Append(update.Text);

                if (sw.Elapsed >= StreamingThrottleInterval)
                {
                    log.Report(new AgentLogEntry
                    {
                        SourceAgent = DisplayName,
                        Text = sb.ToString(),
                        Severity = LogSeverity.Trace,
                        IsStreamingChunk = true
                    });
                    sw.Restart();
                }
            }
        }

        // Emit final chunk
        if (sb.Length > 0)
        {
            log.Report(new AgentLogEntry
            {
                SourceAgent = DisplayName,
                Text = sb.ToString(),
                Severity = LogSeverity.Trace,
                IsStreamingChunk = true
            });
        }

        // Build a ChatResponse from the accumulated text for tool-call detection
        var assistantMessage = new ChatMessage(ChatRole.Assistant, sb.ToString());
        var response = new ChatResponse([assistantMessage]);

        return (response, sb.ToString());
    }

    /// <summary>Default JSON deserialisation — subclasses can override for custom logic.</summary>
    protected TTarget Deserialize<TTarget>(string json)
        => JsonSerializer.Deserialize<TTarget>(json, s_jsonOpts)
           ?? throw new InvalidOperationException($"Failed to deserialize {typeof(TTarget).Name}");

    private void Emit(IProgress<AgentLogEntry> log, string text, LogSeverity sev = LogSeverity.Informational)
        => log.Report(new AgentLogEntry
        {
            SourceAgent = DisplayName,
            Text = text,
            Severity = sev
        });

    private static bool HasFunctionCalls(ChatResponse response)
        => response.Messages.SelectMany(m => m.Contents).OfType<FunctionCallContent>().Any();

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
