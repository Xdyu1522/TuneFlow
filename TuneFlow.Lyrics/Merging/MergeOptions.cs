namespace TuneFlow.Lyrics.Merging;

public sealed class MergeOptions
{
    public int MaxTimeDeltaMs { get; init; } = 100;      // 时间对齐容差
    public bool AllowUnmatched { get; init; } = true;    // 无法匹配的行是否保留
    public MergeType MergeType { get; init; } = MergeType.Translation;
}