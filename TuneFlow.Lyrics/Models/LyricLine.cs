namespace TuneFlow.Lyrics.Models;

public record LyricLine: ILyricLine
{
    public TimeSpan StartTime { get; init; }
    public TimeSpan? EndTime { get; init; }              // 可选，SRT 和增强型 LRC 有
    public string Text { get; init; } = "";
    // 多轨道合并后挂载
    public ILyricLine? Translation { get; init; }
    public ILyricLine? Romanization { get; init; }
    
    public ILyricLine WithTranslation(ILyricLine t)
        => this with { Translation = t };

    public ILyricLine WithRomanization(ILyricLine r)
        => this with { Romanization = r };

    public ILyricLine DetachTrackReferences() => this with { Translation = null, Romanization = null};
}
