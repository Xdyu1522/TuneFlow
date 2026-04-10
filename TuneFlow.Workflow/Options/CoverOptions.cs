namespace TuneFlow.Workflow.Options;

public record CoverOptions : ResourceBaseOptions
{
    public CoverSourceStrategy Strategy { get; init; } = CoverSourceStrategy.NetWorkFirst;
}