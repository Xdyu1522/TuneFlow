using System.Text;
using TuneFlow.Lyrics.Exporting;

namespace TuneFlow.Lyrics.Models;

/// <summary>
/// 表示 LRC 文件头部的元信息标签。
/// 所有字段均可为 null，表示原始文件中未提供该标签。
/// </summary>
public sealed class LyricMeta
{
    /// <summary>[ti:] 歌曲标题</summary>
    public string? Title { get; private set; }

    /// <summary>[ar:] 演唱艺人</summary>
    public string? Artist { get; private set; }

    /// <summary>[al:] 所属专辑</summary>
    public string? Album { get; private set; }

    /// <summary>[au:] 歌词作者</summary>
    public string? Author { get; private set; }

    /// <summary>[by:] LRC 文件制作者</summary>
    public string? Creator { get; private set; }

    /// <summary>[ve:] LRC 工具版本号</summary>
    public string? Version { get; private set; }

    /// <summary>
    /// [offset:] 全局时间偏移，单位毫秒。
    /// 正值表示歌词整体提前，负值表示延后。
    /// 注意：这是 LRC 规范中内嵌的 offset 标签，
    /// 与 <see cref="LyricDocument.GlobalOffset"/> 是两个独立概念——
    /// 解析时会读取此值，但应用偏移由上层逻辑决定。
    /// </summary>
    public int? EmbeddedOffsetMs { get; private set; }

    private readonly Dictionary<string, string> _customTags = new();
    
    /// <summary>
    /// 无法识别的自定义标签，保留原始 key/value 以避免信息丢失。
    /// 例如 [re:LrcMaker]、[tool:xxx] 等非标准标签。
    /// </summary>
    public IReadOnlyDictionary<string, string> CustomTags => _customTags;

    /// <summary>
    /// 判断此元信息是否完全为空（所有标准字段均未提供且无自定义标签）。
    /// </summary>
    public bool IsEmpty =>
        Title is null &&
        Artist is null &&
        Album is null &&
        Author is null &&
        Creator is null &&
        Version is null &&
        EmbeddedOffsetMs is null &&
        CustomTags.Count == 0;

    public void Add(string key, string value)
    {
        switch (key)
        {
            case "ti": Title = value; break;
            case "ar": Artist = value; break;
            case "al": Album = value; break;
            case "au": Author = value; break;
            case "by": Creator = value; break;
            case "ve": Version = value; break;
            case "offset":
                if (int.TryParse(value, out var offset))
                {
                    EmbeddedOffsetMs = offset;
                }
                break;
            default:
                _customTags.Add(key, value);
                break;
        }
    }
    
    public string Export(string lineBreak)
    {
        var lines = new List<string>();

        if (!string.IsNullOrWhiteSpace(Title)) lines.Add($"[ti:{Title}]");
        if (!string.IsNullOrWhiteSpace(Artist)) lines.Add($"[ar:{Artist}]");
        if (!string.IsNullOrWhiteSpace(Album)) lines.Add($"[al:{Album}]");
        if (!string.IsNullOrWhiteSpace(Author)) lines.Add($"[au:{Author}]");
        if (!string.IsNullOrWhiteSpace(Creator)) lines.Add($"[by:{Creator}]");
        if (EmbeddedOffsetMs.HasValue) lines.Add($"[offset:{EmbeddedOffsetMs.Value}]");
        if (!string.IsNullOrWhiteSpace(Version)) lines.Add($"[ve:{Version}]");

        foreach (var (key, value) in _customTags)
        {
            if (!string.IsNullOrWhiteSpace(key) && !string.IsNullOrWhiteSpace(value))
            {
                lines.Add($"[{key}:{value}]");
            }
        }

        return string.Join(lineBreak, lines);
    }

    private void AddToCustom(string key, string value)
    {
        _customTags.TryAdd(key, value);
    }

    public LyricMeta Clone()
    {
        var result = new LyricMeta
        {
            Title = Title,
            Artist = Artist,
            Album = Album,
            Author = Author,
            Creator = Creator,
            Version = Version,
            EmbeddedOffsetMs = EmbeddedOffsetMs,
        };
        foreach (var (key, value) in _customTags)
        {
            result.AddToCustom(key, value);
        }
        return result;
    }
}
