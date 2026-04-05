using System.Collections.Immutable;

namespace TuneFlow.Lyrics.Models;

public record LyricLine
{
    public TimeSpan StartTime { get; init; }
    public TimeSpan? EndTime { get; init; }              // 可选，SRT 和增强型 LRC 有
    public string Text { get; init; } = "";
    public IImmutableList<LyricWord>? Words { get; init; } // null = 逐行模式
    // 多轨道合并后挂载
    public LyricLine? Translation { get; init; }
    public LyricLine? Romanization { get; init; }
    
    public LyricLine WithTranslation(LyricLine t)
        => this with { Translation = t };

    public LyricLine WithRomanization(LyricLine r)
        => this with { Romanization = r };
}