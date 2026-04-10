using System.Threading.Tasks.Dataflow;

namespace TuneFlow.Workflow;

public sealed class WorkflowPipeline
{
    private readonly ITargetBlock<WorkflowContext> _entry;
    private readonly IDataflowBlock _entryBlock;

    internal WorkflowPipeline(ITargetBlock<WorkflowContext> entry, IDataflowBlock entryBlock, Task completion)
    {
        _entry = entry;
        _entryBlock = entryBlock;
        Completion = completion;
    }

    public Task Completion { get; }

    public async Task RunAsync(WorkflowContext context, CancellationToken ct = default)
    {
        var accepted = await _entry.SendAsync(context, ct);
        if (!accepted)
        {
            throw new InvalidOperationException("Workflow pipeline rejected the context.");
        }

        _entryBlock.Complete();
        await Completion;
    }
}
