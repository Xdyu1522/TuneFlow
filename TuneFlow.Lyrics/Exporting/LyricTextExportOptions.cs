using System.Collections.Immutable;
using TuneFlow.Lyrics.Models;

namespace TuneFlow.Lyrics.Exporting;

public record LyricTextExportOptions
{
    public LyricTextExportMode ExportMode { get; init; } = LyricTextExportMode.Interleaved;
    public string LineBreak { get; init; } = "\r\n";
    public ImmutableHashSet<LyricTrackKind> IncludeKinds { get; init; } =
        ImmutableHashSet.Create(LyricTrackKind.Translation, LyricTrackKind.Romanization);
}
