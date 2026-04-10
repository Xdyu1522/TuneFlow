using NcmFox;

namespace TuneFlow.Workflow.Blocks;

public class DecryptBlock
{
    public Task ProcessAsync(WorkflowContext context, CancellationToken ct = default)
    {
        context.NcmFile.Decode(context.OutputPath);
        context.ReportStage(WorkflowStage.Decrypted);
        return Task.CompletedTask;
    }
}