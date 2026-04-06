using System.Collections.Immutable;
using TuneFlow.Lyrics.Merging;
using TuneFlow.Lyrics.Models;
using TuneFlow.Lyrics.Parsing;

namespace TuneFlow.Lyrics;

public static class LyricsFacade
{
    // 自动检测格式并解析
    public static LyricDocument Parse(string content)
    {
        ILyricParser parser = new LrcLineParser();
        return parser.Parse(content);
    }

    // 多轨道合并（原文必须，翻译/罗马音可选）
    public static LyricDocument Merge(
        LyricDocument originLyric,
        LyricDocument mergeLyric,
        MergeOptions options)
    {
        var mergeLines = mergeLyric.Lines.OrderBy(l => l.StartTime).ToImmutableArray();
        var mergeTimes = mergeLines.Select(l => l.StartTime).ToArray();
        var mergedLines = new List<ILyricLine>(originLyric.Lines.Length);

        foreach (var line in originLyric.Lines)
        {
            var bestMatch = TryFindBestMatchByTime(line.StartTime, mergeLines, mergeTimes, options.MaxTimeDeltaMs);
            if (bestMatch is null)
            {
                if (options.AllowUnmatched)
                {
                    mergedLines.Add(line);
                }
                continue;
            }

            // Never attach the same instance back to itself.
            if (ReferenceEquals(line, bestMatch))
            {
                if (options.AllowUnmatched)
                {
                    mergedLines.Add(line);
                }
                continue;
            }

            var detachedMatch = DetachTrackReferences(bestMatch);
            switch (options.MergeType)
            {
                case MergeType.Translation:
                    mergedLines.Add(line.WithTranslation(detachedMatch));
                    break;
                case MergeType.Romanization:
                    mergedLines.Add(line.WithRomanization(detachedMatch));
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        var mergedLyric = new LyricDocument
        {
            GlobalOffset = originLyric.GlobalOffset,
            Meta = originLyric.Meta.Clone(),
            Lines = [..mergedLines]
        };
        return mergedLyric;
    }

    private static ILyricLine? TryFindBestMatchByTime(
        TimeSpan target,
        ImmutableArray<ILyricLine> orderedLines,
        TimeSpan[] orderedTimes,
        int maxTimeDeltaMs)
    {
        if (orderedLines.IsDefaultOrEmpty)
        {
            return null;
        }

        var maxDeltaTicks = TimeSpan.FromMilliseconds(maxTimeDeltaMs).Ticks;
        var index = Array.BinarySearch(orderedTimes, target);
        if (index < 0)
        {
            index = ~index;
        }

        ILyricLine? best = null;
        long bestDelta = long.MaxValue;

        for (var left = index - 1; left >= 0; left--)
        {
            var delta = Math.Abs((orderedTimes[left] - target).Ticks);
            if (delta > maxDeltaTicks)
            {
                break;
            }

            if (delta < bestDelta)
            {
                best = orderedLines[left];
                bestDelta = delta;
            }
        }

        for (var right = index; right < orderedLines.Length; right++)
        {
            var delta = Math.Abs((orderedTimes[right] - target).Ticks);
            if (delta > maxDeltaTicks)
            {
                break;
            }

            if (delta < bestDelta)
            {
                best = orderedLines[right];
                bestDelta = delta;
            }
        }

        return best;
    }

    private static ILyricLine DetachTrackReferences(ILyricLine source)
    {
        return source.DetachTrackReferences();
    }

    // 格式转换
    public static LyricDocument ToLineBased(LyricDocument document) => throw new NotImplementedException();
    public static LyricDocument ToWordBased(LyricDocument document) => throw new NotImplementedException();

    // 序列化
    public static string ToLrc(LyricDocument document) => throw new NotImplementedException();
    public static string ToSrt(LyricDocument document) => throw new NotImplementedException();
    public static string ToJson(LyricDocument document) => throw new NotImplementedException();

    // 偏移（返回新文档，原文档不变）
    public static LyricDocument ApplyOffset(LyricDocument document, TimeSpan offset) => throw new NotImplementedException();
}
