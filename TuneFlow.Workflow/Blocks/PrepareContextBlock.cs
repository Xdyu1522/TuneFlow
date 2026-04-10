using NcmFox;

namespace TuneFlow.Workflow.Blocks;

public class PrepareContextBlock
{
    public Task<WorkflowContext> ProcessAsync(WorkflowRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.SourceFilePath);
        ct.ThrowIfCancellationRequested();

        var ncmFile = NcmDecoder.Open(request.SourceFilePath);
        var builder = WorkflowFactory.CreateBuilder()
            .FromNcmFile(ncmFile)
            .UseLyricsOptions(request.LyricsOptions)
            .UseCoverOptions(request.CoverOptions)
            .UseExecutionOptions(request.ExecutionOptions)
            .WithProgress(request.Progress);

        if (!string.IsNullOrWhiteSpace(request.OutputPath))
        {
            builder.ToOutput(request.OutputPath);
        }
        else if (!string.IsNullOrWhiteSpace(request.OutputDirectory) && !string.IsNullOrWhiteSpace(request.OutputFileName))
        {
            builder.ToOutput(request.OutputDirectory, request.OutputFileName);
        }
        else
        {
            throw new ArgumentException(
                "Either OutputPath or (OutputDirectory + OutputFileName) must be provided.",
                nameof(request));
        }

        var context = builder.Build();

        return Task.FromResult(context);
    }
}
