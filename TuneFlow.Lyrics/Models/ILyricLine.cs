namespace TuneFlow.Lyrics.Models;

public interface ILyricLine
{
    public TimeSpan StartTime { get; init; }
    public TimeSpan? EndTime { get; init; }
    
    public ILyricLine? Translation { get; init; }
    public ILyricLine? Romanization { get; init; }
    
    public string Text { get; init; }
    
    public ILyricLine WithTranslation(ILyricLine t);
    public ILyricLine WithRomanization(ILyricLine r);
    public ILyricLine DetachTrackReferences();
}
