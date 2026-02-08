// elbruno.Doc2Code — computes execution levels for a pipeline DAG using Kahn's algorithm.
namespace elbruno.Doc2Code.Core.Pipeline;

using elbruno.Doc2Code.Core.Models;

/// <summary>
/// Performs topological sorting of a <see cref="PipelineDefinition"/> into execution levels.
/// Steps within the same level can be executed in parallel.
/// </summary>
public sealed class TopologicalSorter
{
    /// <summary>
    /// Returns execution levels where each inner list contains steps that can run in parallel.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when the pipeline contains a cycle.</exception>
    public List<List<PipelineStepDefinition>> Sort(PipelineDefinition pipeline)
    {
        if (pipeline.Steps.Count == 0)
            return [];

        var stepLookup = pipeline.Steps.ToDictionary(s => s.StepId, StringComparer.OrdinalIgnoreCase);
        var inDegree = pipeline.Steps.ToDictionary(s => s.StepId, _ => 0, StringComparer.OrdinalIgnoreCase);
        var outbound = pipeline.Steps.ToDictionary(s => s.StepId, _ => new List<string>(), StringComparer.OrdinalIgnoreCase);

        foreach (var edge in pipeline.Edges)
        {
            if (inDegree.ContainsKey(edge.TargetStepId) && outbound.ContainsKey(edge.SourceStepId))
            {
                inDegree[edge.TargetStepId]++;
                outbound[edge.SourceStepId].Add(edge.TargetStepId);
            }
        }

        var levels = new List<List<PipelineStepDefinition>>();
        var queue = new Queue<string>(pipeline.Steps.Where(s => inDegree[s.StepId] == 0).Select(s => s.StepId));
        var processed = 0;

        while (queue.Count > 0)
        {
            var level = new List<PipelineStepDefinition>();
            var nextQueue = new Queue<string>();

            while (queue.Count > 0)
            {
                var stepId = queue.Dequeue();
                level.Add(stepLookup[stepId]);
                processed++;

                foreach (var next in outbound[stepId])
                {
                    inDegree[next]--;
                    if (inDegree[next] == 0)
                        nextQueue.Enqueue(next);
                }
            }

            levels.Add(level);
            queue = nextQueue;
        }

        if (processed != pipeline.Steps.Count)
            throw new InvalidOperationException("Pipeline contains a cycle and cannot be topologically sorted.");

        return levels;
    }
}
