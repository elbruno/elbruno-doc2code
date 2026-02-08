// elbruno.Doc2Code — validates pipeline definitions for correctness.
namespace elbruno.Doc2Code.Core.Pipeline;

using elbruno.Doc2Code.Core.Models;

/// <summary>Result of pipeline validation.</summary>
public sealed class PipelineValidationResult
{
    public bool IsValid => Errors.Count == 0;
    public List<string> Errors { get; init; } = [];
    public List<string> Warnings { get; init; } = [];
}

/// <summary>
/// Validates a <see cref="PipelineDefinition"/> against a set of <see cref="AgentDefinition"/>s.
/// Checks for missing agents, cycles, connectivity, and unresolved input keys.
/// </summary>
public sealed class PipelineValidator
{
    /// <summary>Validates the pipeline definition.</summary>
    public PipelineValidationResult Validate(PipelineDefinition pipeline, IReadOnlyList<AgentDefinition> agents)
    {
        var errors = new List<string>();
        var warnings = new List<string>();
        var agentLookup = agents.ToDictionary(a => a.AgentKey, StringComparer.OrdinalIgnoreCase);

        if (pipeline.Steps.Count == 0)
        {
            errors.Add("Pipeline has no steps.");
            return new PipelineValidationResult { Errors = errors, Warnings = warnings };
        }

        // Check for duplicate step IDs
        var stepIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var step in pipeline.Steps)
        {
            if (!stepIds.Add(step.StepId))
                errors.Add($"Duplicate step ID: '{step.StepId}'.");
        }

        // Check all step agent keys reference existing agents
        foreach (var step in pipeline.Steps)
        {
            if (!agentLookup.ContainsKey(step.AgentKey))
                errors.Add($"Step '{step.StepId}' references agent '{step.AgentKey}' which does not exist.");
        }

        // Build adjacency for cycle and connectivity checks
        var inbound = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        var outbound = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var step in pipeline.Steps)
        {
            inbound[step.StepId] = [];
            outbound[step.StepId] = [];
        }

        foreach (var edge in pipeline.Edges)
        {
            if (!stepIds.Contains(edge.SourceStepId))
            {
                errors.Add($"Edge source '{edge.SourceStepId}' does not match any step.");
                continue;
            }
            if (!stepIds.Contains(edge.TargetStepId))
            {
                errors.Add($"Edge target '{edge.TargetStepId}' does not match any step.");
                continue;
            }

            outbound[edge.SourceStepId].Add(edge.TargetStepId);
            inbound[edge.TargetStepId].Add(edge.SourceStepId);
        }

        // Root steps (no inbound edges)
        var roots = pipeline.Steps.Where(s => inbound[s.StepId].Count == 0).ToList();
        if (roots.Count == 0)
            errors.Add("Pipeline has no root steps (all steps have inbound edges — possible cycle).");

        // Leaf steps (no outbound edges)
        var leaves = pipeline.Steps.Where(s => outbound[s.StepId].Count == 0).ToList();
        if (leaves.Count == 0)
            errors.Add("Pipeline has no leaf steps (all steps have outbound edges — possible cycle).");

        // Cycle detection via topological sort (Kahn's algorithm)
        var inDegree = pipeline.Steps.ToDictionary(s => s.StepId, s => inbound[s.StepId].Count, StringComparer.OrdinalIgnoreCase);
        var queue = new Queue<string>(roots.Select(r => r.StepId));
        var visited = 0;

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            visited++;
            foreach (var next in outbound[current])
            {
                inDegree[next]--;
                if (inDegree[next] == 0)
                    queue.Enqueue(next);
            }
        }

        if (visited != pipeline.Steps.Count)
            errors.Add("Pipeline contains a cycle.");

        // Check input key satisfaction
        var stepLookup = pipeline.Steps.ToDictionary(s => s.StepId, StringComparer.OrdinalIgnoreCase);
        foreach (var step in pipeline.Steps)
        {
            if (!agentLookup.TryGetValue(step.AgentKey, out var agent))
                continue;

            var availableKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            // Gather output key mappings from upstream edges
            foreach (var edge in pipeline.Edges.Where(e => e.TargetStepId.Equals(step.StepId, StringComparison.OrdinalIgnoreCase)))
            {
                if (!string.IsNullOrWhiteSpace(edge.OutputKeyMapping))
                    availableKeys.Add(edge.OutputKeyMapping);
            }

            foreach (var inputKey in agent.ExpectedInputKeys)
            {
                if (!availableKeys.Contains(inputKey))
                {
                    // Root steps can read "spec" from the initial bag
                    if (inbound[step.StepId].Count == 0 && inputKey.Equals("spec", StringComparison.OrdinalIgnoreCase))
                        continue;

                    warnings.Add($"Step '{step.StepId}' (agent '{step.AgentKey}') expects input key '{inputKey}' but no upstream edge provides it.");
                }
            }
        }

        return new PipelineValidationResult { Errors = errors, Warnings = warnings };
    }
}
