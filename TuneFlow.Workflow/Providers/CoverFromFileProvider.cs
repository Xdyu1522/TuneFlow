using TuneFlow.Workflow.Abstractions;
using TuneFlow.Workflow.Options;

namespace TuneFlow.Workflow.Providers;

public class CoverFromFileProvider: ICoverProvider
{
    public async Task<byte[]?> GetResourceAsync(WorkflowContext context, CancellationToken ct = default)
    {
        return await Task.FromResult(context.NcmFile.CoverData?.Data);
    }

    public CoverSourceStrategy Strategy => CoverSourceStrategy.InFile;
}