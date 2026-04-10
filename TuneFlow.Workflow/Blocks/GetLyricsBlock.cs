using TuneFlow.Lyrics.Exporting;
using TuneFlow.Workflow.Abstractions;

namespace TuneFlow.Workflow.Blocks;

public class GetLyricsBlock(IEnumerable<ILyricsProvider> providers)
{
    public async Task ProcessAsync(WorkflowContext context, CancellationToken ct = default)
    {
        var provider = providers.FirstOrDefault(x => x.Strategy == context.LyricsOptions.Strategy);
        if (provider is null)
        {
            throw new InvalidOperationException($"No lyrics provider found for strategy '{context.LyricsOptions.Strategy}'.");
        }

        var lyric = await provider.GetResourceAsync(context, ct);
        context.LyricsDocument = lyric;
        if (lyric is not null)
        {
            var lyricString = LyricExporter.Export(lyric, new LyricExportOptions
            {
                ExportFormat = context.LyricsOptions.ExportFormat,
                IncludeKinds = context.LyricsOptions.IncludeKinds,
                LineBreak = context.LyricsOptions.LineBreak,
                ExportMode = context.LyricsOptions.ExportMode
            });
            context.ExportedLyric = lyricString;
        }

        context.ReportStage(WorkflowStage.GotLyrics);
    }
}
