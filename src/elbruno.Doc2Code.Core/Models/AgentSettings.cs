// elbruno.Doc2Code — per-agent tuning knobs (prompt, model, temperature).
namespace elbruno.Doc2Code.Core.Models;

/// <summary>Configuration for a single agent in the pipeline.</summary>
[Obsolete("Use AgentDefinition instead. AgentProfile is kept for backward compatibility during migration.")]
public sealed class AgentProfile
{
    public required string AgentKey { get; init; }
    public string Instruction { get; set; } = "";
    public string ModelId { get; set; } = "ministral-3";
    public double Creativity { get; set; } = 0.7;

    /// <summary>Custom system prompt override. When empty, the agent uses its built-in default.</summary>
    public string SystemPrompt { get; set; } = "";
}

/// <summary>Default values for an agent profile (model, system prompt).</summary>
public sealed class AgentDefaultInfo
{
    public required string AgentKey { get; init; }
    public required string DefaultSystemPrompt { get; init; }
}
