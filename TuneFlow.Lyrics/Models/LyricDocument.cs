using System.Collections.Immutable;

namespace TuneFlow.Lyrics.Models;

public sealed class LyricDocument
{
    public LyricMeta Meta { get; init; } = new();
    public ImmutableArray<ILyricLine> Lines { get; init; } = ImmutableArray<ILyricLine>.Empty;
    public LyricTrackKind TrackKind { get; init; } = LyricTrackKind.Original;
    public TimeSpan GlobalOffset { get; init; }          // 全局偏移
}
