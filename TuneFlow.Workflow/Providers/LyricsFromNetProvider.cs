using System.Text.Json;
using System.Text.Json.Serialization;
using TuneFlow.Lyrics;
using TuneFlow.Lyrics.Merging;
using TuneFlow.Lyrics.Models;
using TuneFlow.Workflow.Abstractions;
using TuneFlow.Workflow.Extensions;
using TuneFlow.Workflow.Options;

namespace TuneFlow.Workflow.Providers;

public class LyricsFromNetProvider(HttpClient client) : ILyricsProvider
{
    public async Task<LyricDocument?> GetResourceAsync(WorkflowContext context, CancellationToken ct = default)
    {
        var url = context.NcmFile.LyricUrl;
        if (string.IsNullOrEmpty(url)) return null;
        try
        {
            using var response = await client.GetAsync(url, ct);

            if (!response.IsSuccessStatusCode)
                return null;

            var content = await response.Content.ReadAsStringAsync(ct);
            var parseResult = JsonSerializer.Deserialize<NeteaseLyricResponse>(content);
            var originLyric = parseResult?.Lrc?.Lyric;
            var transLyric = parseResult?.Tlyric?.Lyric;
            var romaLyric = parseResult?.Romalrc?.Lyric;

            if (originLyric is null) return null;

            var originDocument = LyricsFacade.Parse(originLyric);
            var mergedDocument = originDocument;
            if (context.LyricsOptions.IncludeKinds.Contains(LyricTrackKind.Translation) && !string.IsNullOrEmpty(transLyric))
            {
                var transDocument = LyricsFacade.Parse(transLyric);
                mergedDocument = LyricsFacade.Merge(originDocument, transDocument, new MergeOptions{MaxTimeDeltaMs = context.LyricsOptions.MaxTimeDeltaMs , MergeType = MergeType.Translation});
            }
            if (context.LyricsOptions.IncludeKinds.Contains(LyricTrackKind.Romanization) && !string.IsNullOrEmpty(romaLyric))
            {
                var romaDocument = LyricsFacade.Parse(romaLyric);
                mergedDocument = LyricsFacade.Merge(mergedDocument, romaDocument, new MergeOptions{MaxTimeDeltaMs = context.LyricsOptions.MaxTimeDeltaMs , MergeType = MergeType.Romanization});
            }

            return mergedDocument;
        }
        catch (OperationCanceledException)
        {
            throw; // 让上层处理取消
        }
        catch (Exception)
        {
            return null; // 网络错误不要炸 pipeline
        }
    }

    public LyricsSourceStrategy Strategy => LyricsSourceStrategy.NetWork;
}

public class NeteaseLyricResponse
{
    [JsonPropertyName("lrc")]
    public LyricBlock? Lrc { get; set; }

    [JsonPropertyName("tlyric")]
    public LyricBlock? Tlyric { get; set; }

    [JsonPropertyName("romalrc")]
    public LyricBlock? Romalrc { get; set; }
}

public class LyricBlock
{
    [JsonPropertyName("lyric")]
    public string? Lyric { get; set; }
}
