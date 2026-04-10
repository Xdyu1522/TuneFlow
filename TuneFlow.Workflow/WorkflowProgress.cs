namespace TuneFlow.Workflow;

public record WorkflowProgress
{
    public required FileInfo File { get; init; }
    public required string Id { get; init; }
    public required WorkflowStage Stage { get; init; }
    public TimeSpan StepElapsed { get; init; }   // 上一步到这一步的用时
    public TimeSpan TotalElapsed { get; init; }  // 从 Started 到现在的总用时
}