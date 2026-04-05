using System.Text;
using TuneFlow.Lyrics.Models;

namespace TuneFlow.Lyrics.Exporting;

public static class LyricTextExporter
{
    public static string Export(LyricDocument doc, LyricTextExportOptions options)
    {
        var builder = new StringBuilder();
        switch (options.ExportMode)
        {
            case LyricTextExportMode.Interleaved:
                foreach (var line in doc.Lines)
                {
                    builder.Append(line.ExportLrcInterleaved(options.LineBreak, options.IncludeKinds));
                }
                break;
            case LyricTextExportMode.Separated:
                var includeTranslation = options.IncludeKinds.Contains(LyricTrackKind.Translation);
                var includeRomanization = options.IncludeKinds.Contains(LyricTrackKind.Romanization);
                builder.Append(doc.Lines.ExportLrc(options.LineBreak));
                if (includeTranslation)
                {
                    var trans = doc.Lines.Where(l => l.Translation is not null).Select(l => l.Translation!);
                    builder.Append(trans.ExportLrc(options.LineBreak));
                }

                if (includeRomanization)
                {
                    var roman = doc.Lines.Where(l => l.Romanization is not null).Select(l => l.Romanization!);
                    builder.Append(roman.ExportLrc(options.LineBreak));
                }
                break;
        }
        builder.TrimEnd(options.LineBreak);
        return builder.ToString();
    }
}