using System.Text;
using TuneFlow.Lyrics.Models;

namespace TuneFlow.Lyrics.Exporting;

public static class LyricTextExportExtensions
{
    extension(TimeSpan timeSpan)
    {
        private string ToTimeStamp()
        {
            if (timeSpan.TotalMicroseconds == 0) return "00:00.000";
            var milliseconds = timeSpan.Milliseconds.ToString().PadLeft(3, '0');
            var seconds = timeSpan.Seconds.ToString().PadLeft(2, '0');
            var minutes = timeSpan.Minutes.ToString().PadLeft(2, '0');
            if (timeSpan.Hours == 0) return $"{minutes}:{seconds}.{milliseconds}";
            var hours = timeSpan.Hours.ToString().PadLeft(2, '0');
            return $"{hours}:{minutes}:{seconds}.{milliseconds}";
        }
    }

    extension(IEnumerable<LyricLine> lines)
    {
        public string ExportLrc(string lineBreak)
        {
            var builder = new StringBuilder();
            foreach (var line in lines)
            {
                builder.Append(line.ExportLrc()).Append(lineBreak);
            }
            return builder.ToString();
        }
    }
    
    extension(LyricLine line)
    {
        public string ExportLrcInterleaved(string lineBreak, IReadOnlySet<LyricTrackKind> includeKinds)
        {
            var originLyric = line.ExportLrc();
            var includeTranslation = includeKinds.Contains(LyricTrackKind.Translation);
            var includeRomanization = includeKinds.Contains(LyricTrackKind.Romanization);
            string? transLyric = null, romanLyric = null;
            if (includeTranslation && line.Translation is not null)
            {
                transLyric = line.Translation.ExportLrc();
            }

            if (includeRomanization && line.Romanization is not null)
            {
                romanLyric = line.Romanization.ExportLrc();
            }

            var builder = new StringBuilder();
            builder.Append(originLyric).Append(lineBreak);
            if (includeTranslation && !string.IsNullOrEmpty(transLyric))
            {
                builder.Append(transLyric).Append(lineBreak);
            }

            if (includeRomanization && !string.IsNullOrEmpty(romanLyric))
            {
                builder.Append(romanLyric).Append(lineBreak);
            }
            
            return builder.ToString();
        }

        public string ExportLrc()
        {
            var startTimeStamp = line.StartTime.ToTimeStamp();
            var endTimeStamp = line.EndTime?.ToTimeStamp();
            return endTimeStamp is null ? $"[{startTimeStamp}]{line.Text}" : $"[{startTimeStamp}]{line.Text}[{endTimeStamp}]";
        }
    }

    extension(StringBuilder builder)
    {
        public StringBuilder TrimEnd(string target)
        {
            if (builder.Length == 0 || string.IsNullOrEmpty(target)) return builder;

            var targetLen = target.Length;
            if (builder.Length < targetLen) return builder;

            // 验证末尾匹配
            for (var i = 0; i < targetLen; i++)
            {
                if (builder[builder.Length - targetLen + i] != target[i]) return builder;
            }

            builder.Length -= targetLen;
            return builder;
        }
    }
}
