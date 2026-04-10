using System.Collections.Immutable;
using TuneFlow.Lyrics.Models;

namespace TuneFlow.Lyrics.Exporting;

public record LyricExportOptions
{
    public ExportFormat ExportFormat;
    public ExportMode ExportMode { get; init; } = ExportMode.Interleaved;
    public string LineBreak { get; init; } = "\r\n";
    public ImmutableHashSet<LyricTrackKind> IncludeKinds { get; init; } =
        ImmutableHashSet.Create(LyricTrackKind.Translation, LyricTrackKind.Romanization);
}
