namespace TuneFlow.Workflow.Options;

public record BatchOptions
{
    public int MaxDegreeOfParallelism { get; init; } = Environment.ProcessorCount;
    public int BoundedCapacity { get; init; } = 100;
}
