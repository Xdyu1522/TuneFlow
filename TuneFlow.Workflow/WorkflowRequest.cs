using TuneFlow.Workflow.Options;

namespace TuneFlow.Workflow;

public record WorkflowRequest
{
    public required string SourceFilePath { get; init; }
    public string? OutputPath { get; init; }
    public string? OutputDirectory { get; init; }
    public string? OutputFileName { get; init; }
    public LyricsOptions LyricsOptions { get; init; } = new();
    public CoverOptions CoverOptions { get; init; } = new();
    public WorkflowExecutionOptions ExecutionOptions { get; init; } = new();
    public IProgress<WorkflowProgress>? Progress { get; init; }
}
