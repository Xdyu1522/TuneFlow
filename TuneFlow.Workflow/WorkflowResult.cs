namespace TuneFlow.Workflow;

public record WorkflowResult
{
    public required string SourceFilePath { get; init; }
    public string? OutputFilePath { get; init; }
    public string? LyricsFilePath { get; init; }
    public string? CoverFilePath { get; init; }
    public Exception? Error { get; init; }

    public bool IsSuccess => Error is null;

    internal static WorkflowResult Success(
        string sourceFilePath,
        string outputFilePath,
        string? lyricsFilePath,
        string? coverFilePath)
    {
        return new WorkflowResult
        {
            SourceFilePath = sourceFilePath,
            OutputFilePath = outputFilePath,
            LyricsFilePath = lyricsFilePath,
            CoverFilePath = coverFilePath
        };
    }

    internal static WorkflowResult Failure(string sourceFilePath, Exception error)
    {
        return new WorkflowResult
        {
            SourceFilePath = sourceFilePath,
            Error = error
        };
    }
}
