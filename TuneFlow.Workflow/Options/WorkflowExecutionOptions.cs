using System.Threading.Tasks.Dataflow;

namespace TuneFlow.Workflow.Options;

public record WorkflowExecutionOptions
{
    public int MaxDegreeOfParallelism { get; init; } = 4;
    public bool EnsureOrdered { get; init; } = true;
    public int BoundedCapacity { get; init; } = DataflowBlockOptions.Unbounded;
    public int MaxMessagesPerTask { get; init; } = DataflowBlockOptions.Unbounded;
}
