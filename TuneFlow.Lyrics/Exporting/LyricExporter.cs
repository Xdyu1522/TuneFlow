using TuneFlow.Lyrics.Models;

namespace TuneFlow.Lyrics.Exporting;

public static class LyricExporter
{
    private static readonly IExportWriter[] Writers =
    [
        new LrcExportWriter()
    ];

    public static string Export(LyricDocument doc, LyricExportOptions options)
    {
        var writer = Writers.FirstOrDefault(w => w.Format == options.ExportFormat);
        if (writer is null)
        {
            throw new NotSupportedException($"Export format '{options.ExportFormat}' is not supported.");
        }

        return writer.Write(doc, options);
    }
}
