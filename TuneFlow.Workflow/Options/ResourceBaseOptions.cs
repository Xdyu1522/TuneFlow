namespace TuneFlow.Workflow.Options;

public record ResourceBaseOptions
{
    public bool Embed { get; init; }
    public bool SaveToFile { get; init; }
    public bool ShouldGet => Embed || SaveToFile;
    public string? SavePath { get; init; }
}