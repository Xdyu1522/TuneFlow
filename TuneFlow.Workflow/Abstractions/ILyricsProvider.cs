using TuneFlow.Lyrics.Models;
using TuneFlow.Workflow.Options;

namespace TuneFlow.Workflow.Abstractions;

public interface ILyricsProvider: IResourcesProvider<LyricDocument>
{
    public LyricsSourceStrategy Strategy { get; }
}