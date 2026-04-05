using System.Collections.Immutable;
using Parlot;
using Parlot.Fluent;
using TuneFlow.Lyrics.Models;

namespace TuneFlow.Lyrics.Parsing;

public class LrcLineParser: ILyricParser
{
    private static readonly Parser<object?> AnyLine =
        OneOf(
            LrcGrammar.MetaTag.Then(m => (object?)m),
            LrcGrammar.LyricLine.Then(l => (object?)l),
            AnyCharBefore(LrcGrammar.NewLine).Then(object? (_) => null)
        );
    
    public LyricDocument Parse(string content)
    {
        var meta = new LyricMeta();
        var lines = new List<LyricLine>();

        foreach (var rawLine in content.Split('\n'))
        {
            var line = rawLine.TrimEnd("\r").ToString();
            if (string.IsNullOrWhiteSpace(line)) continue;
            
            if (LrcGrammar.LyricLine.TryParse(line, out var lyricResult))
            {
                lines.AddRange(lyricResult.Times.Select(t => new LyricLine { StartTime = t, Text = lyricResult.Text }));
            }
            else if (LrcGrammar.MetaTag.TryParse(line, out var metaResult))
            {
                meta.Add(metaResult.Key, metaResult.Value);
            }
        }
        
        lines.Sort((a, b) => a.StartTime.CompareTo(b.StartTime));
        return new LyricDocument { Meta = meta, Lines = [..lines] };
    }
    public bool CanParse(string content)
    {
        throw new NotImplementedException();
    }
}
