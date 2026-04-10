using TuneFlow.Lyrics.Exporting;

namespace TuneFlow.Workflow.Blocks;

public class SaveToFileBlock
{
    public async Task ProcessAsync(WorkflowContext context, CancellationToken ct = default)
    {
        var saved = false;
        if (context.LyricsOptions.SaveToFile && context.LyricsDocument is not null)
        {
            if (context.LyricsOptions.SavePath is null) throw new Exception("Lyric save path is missing");

            await File.WriteAllTextAsync(context.LyricsOptions.SavePath, context.ExportedLyric, ct);
            saved = true;
        }
        if (context.CoverOptions.SaveToFile && context.CoverData is not null)
        {
            if (context.CoverOptions.SavePath is null) throw new Exception("Cover save path is missing");
            await File.WriteAllBytesAsync(context.CoverOptions.SavePath, context.CoverData, ct);
            saved = true;
        }

        if (saved)
        {
            context.ReportStage(WorkflowStage.SavedToFile);
        }
    }
}
