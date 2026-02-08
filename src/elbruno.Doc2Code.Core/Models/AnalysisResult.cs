// elbruno.Doc2Code — output of the Analyst agent: entities, actors, rules, and state machines extracted from the spec.
namespace elbruno.Doc2Code.Core.Models;

/// <summary>Result produced by the analysis stage.</summary>
public sealed class AnalysisResult
{
    public List<DomainEntity> DomainEntities { get; init; } = [];
    public List<SystemActor> SystemActors { get; init; } = [];
    public List<DomainRule> DomainRules { get; init; } = [];
    public List<StateFlow> StateFlows { get; init; } = [];
    public string OverviewNotes { get; set; } = "";
}

/// <summary>An entity discovered in the requirements.</summary>
public sealed class DomainEntity
{
    public required string Label { get; init; }
    public string Explanation { get; set; } = "";
    public List<FieldSpec> Fields { get; init; } = [];
    public List<string> LinkedEntities { get; init; } = [];
}

/// <summary>Describes a single property/field of a domain entity.</summary>
public sealed class FieldSpec
{
    public required string FieldName { get; init; }
    public required string DataType { get; init; }
    public bool Mandatory { get; init; }
    public string? Notes { get; init; }
}

/// <summary>An actor (user role) that interacts with the system.</summary>
public sealed class SystemActor
{
    public required string RoleName { get; init; }
    public string Purpose { get; set; } = "";
    public List<string> Capabilities { get; init; } = [];
}

/// <summary>A business rule extracted from the specification.</summary>
public sealed class DomainRule
{
    public required string RuleCode { get; init; }
    public string RuleText { get; set; } = "";
    public string WhenClause { get; set; } = "";
    public string ThenClause { get; set; } = "";
}

/// <summary>A state machine (lifecycle) for an entity.</summary>
public sealed class StateFlow
{
    public required string OwnerEntity { get; init; }
    public List<FlowState> FlowStates { get; init; } = [];
    public List<FlowTransition> FlowTransitions { get; init; } = [];
}

/// <summary>A single state within a <see cref="StateFlow"/>.</summary>
public sealed class FlowState
{
    public required string StateName { get; init; }
    public bool Entry { get; init; }
    public bool Terminal { get; init; }
}

/// <summary>A transition between two states.</summary>
public sealed class FlowTransition
{
    public required string Origin { get; init; }
    public required string Destination { get; init; }
    public required string Event { get; init; }
    public string? Precondition { get; init; }
}
