using System.Threading.Tasks.Dataflow;
using TuneFlow.Workflow.Blocks;
using TuneFlow.Workflow.Options;

namespace TuneFlow.Workflow;

public class WorkflowFactory(
    StartBlock startBlock,
    DecryptBlock decryptBlock,
    GetLyricsBlock getLyricsBlock,
    GetCoverBlock getCoverBlock,
    EmbedBlock embedBlock,
    SaveToFileBlock saveToFileBlock)
{
    public static WorkflowContextBuilder CreateBuilder() => WorkflowContextBuilder.Create();

    public WorkflowPipeline CreatePipeline(WorkflowContext context, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        var steps = new List<IPropagatorBlock<WorkflowContext, WorkflowContext>>
        {
            CreateStep(startBlock.ProcessAsync, context.ExecutionOptions, ct),
            CreateStep(decryptBlock.ProcessAsync, context.ExecutionOptions, ct)
        };

        if (context.LyricsOptions.ShouldGet)
        {
            steps.Add(CreateStep(getLyricsBlock.ProcessAsync, context.ExecutionOptions, ct));
        }

        if (context.CoverOptions.ShouldGet)
        {
            steps.Add(CreateStep(getCoverBlock.ProcessAsync, context.ExecutionOptions, ct));
        }

        if (context.LyricsOptions.Embed || context.CoverOptions.Embed)
        {
            steps.Add(CreateStep(embedBlock.ProcessAsync, context.ExecutionOptions, ct));
        }

        if (context.LyricsOptions.SaveToFile || context.CoverOptions.SaveToFile)
        {
            steps.Add(CreateStep(saveToFileBlock.ProcessAsync, context.ExecutionOptions, ct));
        }

        var linkOptions = new DataflowLinkOptions { PropagateCompletion = true };
        for (var i = 0; i < steps.Count - 1; i++)
        {
            steps[i].LinkTo(steps[i + 1], linkOptions);
        }

        var finalBlock = new ActionBlock<WorkflowContext>(
            ctx => ctx.ReportStage(WorkflowStage.Finished),
            CreateExecutionOptions(context.ExecutionOptions, ct));

        steps[^1].LinkTo(finalBlock, linkOptions);

        return new WorkflowPipeline(steps[0], steps[0], finalBlock.Completion);
    }

    private static TransformBlock<WorkflowContext, WorkflowContext> CreateStep(
        Func<WorkflowContext, CancellationToken, Task> handler,
        WorkflowExecutionOptions workflowOptions,
        CancellationToken ct)
    {
        return new TransformBlock<WorkflowContext, WorkflowContext>(
            async ctx =>
            {
                await handler(ctx, ct);
                return ctx;
            },
            CreateExecutionOptions(workflowOptions, ct));
    }

    private static ExecutionDataflowBlockOptions CreateExecutionOptions(WorkflowExecutionOptions workflowOptions, CancellationToken ct)
    {
        return new ExecutionDataflowBlockOptions
        {
            CancellationToken = ct,
            MaxDegreeOfParallelism = workflowOptions.MaxDegreeOfParallelism,
            EnsureOrdered = workflowOptions.EnsureOrdered,
            BoundedCapacity = workflowOptions.BoundedCapacity,
            MaxMessagesPerTask = workflowOptions.MaxMessagesPerTask
        };
    }
}
