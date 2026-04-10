namespace TuneFlow.Workflow.Blocks;

using TuneFlow.Workflow;

public class StartBlock
{
    public Task ProcessAsync(WorkflowContext context, CancellationToken ct = default)
    {
        // 基础校验
        if (!context.File.Exists)
            throw new FileNotFoundException("Input file not found.", context.File.FullName);

        if (context.NcmFile.MetaData is null)
            throw new InvalidOperationException("NCM metadata is missing.");

        // 可选：校验输出路径
        if (string.IsNullOrWhiteSpace(context.OutputPath))
            throw new InvalidOperationException("Output path is not specified.");

        // 可选：确保输出目录存在
        var dir = Path.GetDirectoryName(context.OutputPath);
        if (!string.IsNullOrEmpty(dir))
        {
            Directory.CreateDirectory(dir);
        }
        
        // 上报开始
        context.ReportStage(WorkflowStage.Started);

        return Task.CompletedTask;
    }
}