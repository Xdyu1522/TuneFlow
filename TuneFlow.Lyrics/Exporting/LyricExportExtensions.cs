namespace TuneFlow.Lyrics.Exporting;

public static class LyricExportExtensions
{
    extension(TimeSpan timeSpan)
    {
        public string ToTimeStamp()
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
}
