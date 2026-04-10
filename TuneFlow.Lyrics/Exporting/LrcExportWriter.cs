using TuneFlow.Lyrics.Models;

namespace TuneFlow.Lyrics.Exporting;

public sealed class LrcExportWriter : IExportWriter
{
    public ExportFormat Format => ExportFormat.Lrc;

    public string Write(LyricDocument document, LyricExportOptions options)
    {
        var results = new List<string>();

        var meta = document.Meta.Export(options.LineBreak);
        if (!string.IsNullOrWhiteSpace(meta))
        {
            results.Add(meta);
        }

        switch (options.ExportMode)
        {
            case ExportMode.Interleaved:
                results.AddRange(document.Lines.Select(line => WriteInterleaved(line, options)));
                break;
            case ExportMode.Separated:
                results.Add(WriteBlock(document.Lines, options.LineBreak));
                if (options.IncludeKinds.Contains(LyricTrackKind.Translation))
                {
                    results.Add(WriteBlock(document.Lines.Where(l => l.Translation is not null).Select(l => l.Translation!), options.LineBreak));
                }

                if (options.IncludeKinds.Contains(LyricTrackKind.Romanization))
                {
                    results.Add(WriteBlock(document.Lines.Where(l => l.Romanization is not null).Select(l => l.Romanization!), options.LineBreak));
                }
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(options.ExportMode));
        }

        return string.Join(options.LineBreak, results.Where(r => !string.IsNullOrWhiteSpace(r)));
    }

    private static string WriteInterleaved(ILyricLine line, LyricExportOptions options)
    {
        var lines = new List<string> { WriteLine(line) };

        if (options.IncludeKinds.Contains(LyricTrackKind.Translation) && line.Translation is not null && !string.IsNullOrWhiteSpace(line.Translation.Text))
        {
            lines.Add(WriteLine(line.Translation));
        }

        if (options.IncludeKinds.Contains(LyricTrackKind.Romanization) && line.Romanization is not null && !string.IsNullOrWhiteSpace(line.Romanization.Text))
        {
            lines.Add(WriteLine(line.Romanization));
        }

        return string.Join(options.LineBreak, lines);
    }

    private static string WriteBlock(IEnumerable<ILyricLine> lines, string lineBreak)
    {
        return string.Join(lineBreak, lines.Select(WriteLine));
    }

    private static string WriteLine(ILyricLine line)
    {
        var start = line.StartTime.ToTimeStamp();
        if (line.EndTime is { } end)
        {
            return $"[{start}]{line.Text}[{end.ToTimeStamp()}]";
        }

        return $"[{start}]{line.Text}";
    }
}
