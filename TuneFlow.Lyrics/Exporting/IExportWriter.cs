using TuneFlow.Lyrics.Models;

namespace TuneFlow.Lyrics.Exporting;

public interface IExportWriter
{
    ExportFormat Format { get; }
    string Write(LyricDocument document, LyricExportOptions options);
}
