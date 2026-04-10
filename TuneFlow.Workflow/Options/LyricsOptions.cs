using System.Collections.Immutable;
using TuneFlow.Lyrics.Exporting;
using TuneFlow.Lyrics.Models;

namespace TuneFlow.Workflow.Options;

public record LyricsOptions: ResourceBaseOptions
{
    public ExportFormat ExportFormat { get; init; } = ExportFormat.Lrc;
    public ExportMode ExportMode { get; init; } = ExportMode.Interleaved;
    public int MaxTimeDeltaMs { get; init; } = 10;
    public string LineBreak { get; init; } = "\r\n";
    public ImmutableHashSet<LyricTrackKind> IncludeKinds { get; init; } = ImmutableHashSet.Create(LyricTrackKind.Translation);
    public LyricsSourceStrategy Strategy { get; init; } = LyricsSourceStrategy.NetWork;
}