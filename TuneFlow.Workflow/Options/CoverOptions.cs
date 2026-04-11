namespace TuneFlow.Workflow.Options;

public record CoverOptions
{
    public bool Embed { get; init; }
    public bool SaveToFile { get; init; }
    public bool ShouldGet => Embed || SaveToFile;

    public CoverSourceStrategy Strategy { get; init; } = CoverSourceStrategy.NetWorkFirst;
}
