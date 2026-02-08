// elbruno.Doc2Code — domain-specific observability helpers for the agent pipeline.
// Generic Aspire service defaults (OpenTelemetry, health checks, resilience) live in Extensions.cs.
namespace elbruno.Doc2Code.ServiceDefaults;

using System.Diagnostics;

/// <summary>
/// elbruno.Doc2Code-specific observability helpers for agent-pipeline tracing.
/// The <see cref="PipelineTracer"/> ActivitySource is automatically registered with
/// OpenTelemetry via <c>AddServiceDefaults()</c> in <see cref="Extensions"/>.
/// </summary>
public static class Doc2CodeObservability
{
    /// <summary>Dedicated <see cref="ActivitySource"/> for agent-pipeline spans.</summary>
    public static readonly ActivitySource PipelineTracer =
        new("elbruno.Doc2Code.AgentPipeline", "0.1.0-dev");

    /// <summary>Starts a tracing span scoped to a particular agent step; dispose when done.</summary>
    public static Activity? StartAgentSpan(string agentLabel, string step)
    {
        var activity = PipelineTracer.StartActivity($"{agentLabel}:{step}", ActivityKind.Internal);
        return activity;
    }

    /// <summary>Starts a tracing span with pipeline-level metadata attributes.</summary>
    public static Activity? StartAgentSpan(string agentLabel, string step,
        int stepIndex, int totalSteps, int level, string pipelineName)
    {
        var activity = PipelineTracer.StartActivity($"{agentLabel}:{step}", ActivityKind.Internal);
        if (activity is not null)
        {
            activity.SetTag("pipeline.step.index", stepIndex);
            activity.SetTag("pipeline.total_steps", totalSteps);
            activity.SetTag("pipeline.level", level);
            activity.SetTag("pipeline.definition.name", pipelineName);
        }
        return activity;
    }
}

