// elbruno.Doc2Code — defines the structure of a user-configurable pipeline as a DAG.
namespace elbruno.Doc2Code.Core.Models;

/// <summary>
/// A complete pipeline definition describing a directed acyclic graph (DAG) of agent steps.
/// </summary>
public sealed class PipelineDefinition
{
    /// <summary>Unique identifier (GUID string).</summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>Human-readable pipeline name.</summary>
    public string Name { get; set; } = "";

    /// <summary>Short description of what this pipeline does.</summary>
    public string Description { get; set; } = "";

    /// <summary>Whether this is the factory-shipped default pipeline.</summary>
    public bool IsDefault { get; set; }

    /// <summary>Whether this pipeline is currently active for generation runs.</summary>
    public bool IsActive { get; set; }

    /// <summary>Auto-incremented version number, bumped on each save.</summary>
    public int Version { get; set; } = 1;

    /// <summary>UTC timestamp of creation.</summary>
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    /// <summary>UTC timestamp of the last update.</summary>
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    /// <summary>Ordered list of steps in this pipeline.</summary>
    public List<PipelineStepDefinition> Steps { get; set; } = [];

    /// <summary>Edges connecting steps to form the DAG.</summary>
    public List<PipelineEdge> Edges { get; set; } = [];
}

/// <summary>A single step in a pipeline, referencing an <see cref="AgentDefinition"/>.</summary>
public sealed class PipelineStepDefinition
{
    /// <summary>Well-known step ID for the Document Input bookend.</summary>
    public const string DocumentInputStepId = "step-document-input";

    /// <summary>Well-known step ID for the Generated Assets bookend.</summary>
    public const string GeneratedAssetsStepId = "step-generated-assets";

    /// <summary>Unique step identifier within the pipeline.</summary>
    public string StepId { get; set; } = Guid.NewGuid().ToString();

    /// <summary>Key of the <see cref="AgentDefinition"/> to execute.</summary>
    public string AgentKey { get; set; } = "";

    /// <summary>Optional retry policy for this step.</summary>
    public StepRetryPolicy? RetryPolicy { get; set; }

    /// <summary>X coordinate on the designer canvas.</summary>
    public double PositionX { get; set; }

    /// <summary>Y coordinate on the designer canvas.</summary>
    public double PositionY { get; set; }

    /// <summary>
    /// True for the two fixed bookend steps (Document Input / Generated Assets).
    /// Bookend steps cannot be removed from a pipeline.
    /// </summary>
    public bool IsBookend { get; set; }
}

/// <summary>Retry policy for a pipeline step with a quality gate.</summary>
public sealed class StepRetryPolicy
{
    /// <summary>Maximum number of retries before failing.</summary>
    public int MaxRetries { get; set; } = 2;

    /// <summary>JSON field name in the output to check (e.g. "scoreOutOf100").</summary>
    public string QualityGateField { get; set; } = "";

    /// <summary>Minimum acceptable value for the quality gate field.</summary>
    public int AcceptanceThreshold { get; set; } = 70;
}

/// <summary>A directed edge connecting two pipeline steps.</summary>
public sealed class PipelineEdge
{
    /// <summary>Step ID of the source (upstream) step.</summary>
    public string SourceStepId { get; set; } = "";

    /// <summary>Step ID of the target (downstream) step.</summary>
    public string TargetStepId { get; set; } = "";

    /// <summary>Which output key the target reads from the source's output.</summary>
    public string OutputKeyMapping { get; set; } = "";
}
