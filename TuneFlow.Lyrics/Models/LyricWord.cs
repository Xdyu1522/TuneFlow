namespace TuneFlow.Lyrics.Models;

public record LyricWord
{
    public TimeSpan StartTime { get; init; }
    public TimeSpan? EndTime { get; init; }
    public string Text { get; init; } = "";
}