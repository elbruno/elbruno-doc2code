// elbruno.Doc2Code — architectural design output produced by the Architect agent.
namespace elbruno.Doc2Code.Core.Models;

/// <summary>Blueprint describing the target .NET solution layout.</summary>
public sealed class ArchitectureBlueprint
{
    public required string SolutionName { get; init; }
    public List<ModuleSpec> Modules { get; init; } = [];
    public List<string> DesignPatterns { get; init; } = [];
    public List<ModuleLink> ModuleLinks { get; init; } = [];
    public string DesignNotes { get; set; } = "";
}

/// <summary>A project / module inside the generated solution.</summary>
public sealed class ModuleSpec
{
    public required string ModuleName { get; init; }
    public required string ArchLayer { get; init; }
    public string Purpose { get; set; } = "";
    public List<string> ProjectReferences { get; init; } = [];
    public List<string> PackageReferences { get; init; } = [];
}

/// <summary>A directed dependency between two modules.</summary>
public sealed class ModuleLink
{
    public required string SourceModule { get; init; }
    public required string TargetModule { get; init; }
    public string LinkKind { get; set; } = "reference";
}
