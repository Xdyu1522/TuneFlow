namespace TuneFlow.Workflow.Abstractions;

public interface IResourcesProvider<T>
{
    public Task<T?> GetResourceAsync(WorkflowContext context, CancellationToken ct = default);
}