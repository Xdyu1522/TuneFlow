using TuneFlow.Workflow.Abstractions;
using TuneFlow.Workflow.Options;

namespace TuneFlow.Workflow.Providers;

public class CoverFromNetProvider(HttpClient client): ICoverProvider
{
    public async Task<byte[]?> GetResourceAsync(WorkflowContext context, CancellationToken ct = default)
    {
        var url = context.NcmFile.MetaData?.AlbumCoverUrl;
        if (url is null) return null;
        return await CoverDownloader.DownloadCover(client, url, ct);
    }

    public CoverSourceStrategy Strategy => CoverSourceStrategy.NetWork;
}