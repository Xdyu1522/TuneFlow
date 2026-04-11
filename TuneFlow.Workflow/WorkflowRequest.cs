using TuneFlow.Workflow.Options;

namespace TuneFlow.Workflow;

public record WorkflowRequest
{
    public required string SourceFilePath { get; init; }
    public required string OutputDirectory { get; init; }
    public LyricsOptions LyricsOptions { get; init; } = new();
    public CoverOptions CoverOptions { get; init; } = new();
    public IProgress<WorkflowProgress>? Progress { get; init; }
}
