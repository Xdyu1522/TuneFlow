using System.Threading.Tasks.Dataflow;

namespace TuneFlow.Workflow;

public class WorkflowRunner(WorkflowFactory workflowFactory, Blocks.PrepareContextBlock prepareContextBlock)
{
    public async Task RunAsync(WorkflowContext context, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        var pipeline = workflowFactory.CreatePipeline(context, ct);
        await pipeline.RunAsync(context, ct);
    }

    public async Task RunAsync(Action<WorkflowContextBuilder> configure, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(configure);
        var builder = WorkflowFactory.CreateBuilder();
        configure(builder);
        var context = builder.Build();
        await RunAsync(context, ct);
    }

    public async Task RunAsync(WorkflowRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var prepareBlock = new TransformBlock<WorkflowRequest, WorkflowContext>(
            req => prepareContextBlock.ProcessAsync(req, ct),
            CreateExecutionOptions(request.ExecutionOptions, ct));

        var runBlock = new ActionBlock<WorkflowContext>(
            ctx => RunAsync(ctx, ct),
            CreateExecutionOptions(request.ExecutionOptions, ct));

        prepareBlock.LinkTo(runBlock, new DataflowLinkOptions { PropagateCompletion = true });

        var accepted = await prepareBlock.SendAsync(request, ct);
        if (!accepted)
        {
            throw new InvalidOperationException("PrepareContextBlock rejected the workflow request.");
        }

        prepareBlock.Complete();
        await runBlock.Completion;
    }

    private static ExecutionDataflowBlockOptions CreateExecutionOptions(
        Options.WorkflowExecutionOptions options,
        CancellationToken ct)
    {
        return new ExecutionDataflowBlockOptions
        {
            CancellationToken = ct,
            MaxDegreeOfParallelism = options.MaxDegreeOfParallelism,
            EnsureOrdered = options.EnsureOrdered,
            BoundedCapacity = options.BoundedCapacity,
            MaxMessagesPerTask = options.MaxMessagesPerTask
        };
    }
}
