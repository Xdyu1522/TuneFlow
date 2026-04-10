using TuneFlow.Workflow.Abstractions;
using TuneFlow.Workflow.Options;

namespace TuneFlow.Workflow.Blocks;

public class GetCoverBlock(IEnumerable<ICoverProvider> coverProviders)
{
    public async Task ProcessAsync(WorkflowContext context, CancellationToken ct = default)
    {
        var orderedProviders = context.CoverOptions.Strategy switch
        {
            CoverSourceStrategy.NetWork => PickInOrder(CoverSourceStrategy.NetWork),
            CoverSourceStrategy.InFile => PickInOrder(CoverSourceStrategy.InFile),
            CoverSourceStrategy.NetWorkFirst => PickInOrder(CoverSourceStrategy.NetWork, CoverSourceStrategy.InFile),
            CoverSourceStrategy.InFileFirst => PickInOrder(CoverSourceStrategy.InFile, CoverSourceStrategy.NetWork),
            _ => throw new ArgumentOutOfRangeException()
        };

        byte[]? cover = null;
        foreach (var provider in orderedProviders)
        {
            cover = await provider.GetResourceAsync(context, ct);
            if (cover is not null && cover.Length > 0)
            {
                break;
            }
        }

        context.CoverData = cover;
        context.ReportStage(WorkflowStage.GotCover);
    }

    private IEnumerable<ICoverProvider> PickInOrder(params CoverSourceStrategy[] order)
    {
        foreach (var strategy in order)
        {
            var provider = coverProviders.FirstOrDefault(x => x.Strategy == strategy);
            if (provider is not null)
            {
                yield return provider;
            }
        }
    }
}
