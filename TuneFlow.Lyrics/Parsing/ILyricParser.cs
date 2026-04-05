using TuneFlow.Lyrics.Models;

namespace TuneFlow.Lyrics.Parsing;

public interface ILyricParser
{
    bool CanParse(string content);
    LyricDocument Parse(string content);
}