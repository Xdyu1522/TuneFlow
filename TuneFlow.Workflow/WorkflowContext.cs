using System.Diagnostics;
using NcmFox.Models;
using TuneFlow.Lyrics.Models;
using TuneFlow.Workflow.Options;

namespace TuneFlow.Workflow;

public record WorkflowContext
{
    public required FileInfo File { get; init; }
    public required NcmFile NcmFile { get; init; }
    public required string OutputPath { get; init; }
    public LyricsOptions LyricsOptions { get; init; } = new LyricsOptions();
    public CoverOptions CoverOptions { get; init; } = new CoverOptions();
    public WorkflowExecutionOptions ExecutionOptions { get; init; } = new WorkflowExecutionOptions();

    public byte[]? CoverData { get; set; }
    public LyricDocument? LyricsDocument { get; set; }
    public string? ExportedLyric { get; set; }
    
    public IProgress<WorkflowProgress>? Progress { get; init; }
    
    private readonly WorkflowTimer _timer = new();

    public void ReportStage(WorkflowStage stage)
    {
        var (step, total) = _timer.Next();

        Progress?.Report(new WorkflowProgress
            {
                File = NcmFile.FileInfo,
                Id = NcmFile.MetaData?.Id!,
                Stage = stage,
                StepElapsed = step,
                TotalElapsed = total,
            }
        );
    }
}

internal class WorkflowTimer
{
    private readonly Stopwatch _total = Stopwatch.StartNew();
    private TimeSpan _lastStageTime = TimeSpan.Zero;

    public (TimeSpan step, TimeSpan total) Next()
    {
        var now = _total.Elapsed;
        var step = now - _lastStageTime;
        _lastStageTime = now;
        return (step, now);
    }
}
