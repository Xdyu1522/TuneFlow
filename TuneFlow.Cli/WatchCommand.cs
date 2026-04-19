using System.Collections.Concurrent;
using System.ComponentModel;
using System.Threading.Channels;
using Spectre.Console;
using Spectre.Console.Cli;
using Spectre.Console.Rendering;
using TuneFlow.Workflow;
using TuneFlow.Workflow.Options;

namespace TuneFlow.Cli;

public class WatchCommand(WorkflowRunner runner) : Command<WatchCommand.Settings>
{
    private readonly ConcurrentQueue<CompletedFile> _completedFiles = new();
    private readonly ConcurrentDictionary<string, CancellationTokenSource> _ctsMap = new();
    private readonly ConcurrentDictionary<string, FileProgress> _progressMap = new();
    private Settings? _settings;

    protected override int Execute(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        if (!Path.Exists(settings.Path))
        {
            AnsiConsole.MarkupLine($"[red]路径不存在: {settings.Path}[/]");
            return -1;
        }

        _settings = settings;
        if (!Path.Exists(settings.SavePath)) Directory.CreateDirectory(settings.SavePath);

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            cts.Cancel();
        };

        var token = cts.Token;

        var channel = Channel.CreateBounded<WorkflowRequest>(new BoundedChannelOptions(100)
        {
            SingleReader = false,
            SingleWriter = true
        });

        var progressReporter = new Progress<WorkflowProgress>(p =>
        {
            _progressMap.AddOrUpdate(
                p.File.FullName,
                _ => new FileProgress(p),
                (_, existing) =>
                {
                    existing.Update(p);
                    return existing;
                });
        });

        var streamTask = runner.RunStreamAsync(
            channel.Reader,
            result =>
            {
                _progressMap.TryRemove(result.SourceFilePath, out _);

                if (result.IsSuccess)
                    _completedFiles.Enqueue(new CompletedFile(
                        Path.GetFileName(result.SourceFilePath),
                        result.OutputFilePath ?? ""));
                else
                    _completedFiles.Enqueue(new CompletedFile(
                        Path.GetFileName(result.SourceFilePath),
                        ErrorMessage: result.Error?.Message ?? "未知错误"));
            },
            4,
            token);

        using var watcher = new FileSystemWatcher();
        watcher.Path = settings.Path;
        watcher.IncludeSubdirectories = true;
        watcher.Filters.Add("*.mp3");
        watcher.Filters.Add("*.flac");
        watcher.Filters.Add("*.ncm");
        watcher.EnableRaisingEvents = true;

        watcher.Changed += (s, e) =>
        {
            var path = e.FullPath;

            if (_ctsMap.TryRemove(path, out var oldCts))
            {
                oldCts.Cancel();
                oldCts.Dispose();
            }

            var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(token);
            _ctsMap.AddOrUpdate(path, linkedCts, (_, __) => linkedCts);

            _ = DebounceAndEnqueueAsync(path, channel.Writer, progressReporter, linkedCts.Token);
        };

        AnsiConsole.MarkupLine("[bold blue]TuneFlow Watch[/] - 监视文件更改中...\n");

        try
        {
            AnsiConsole.Live(new Markup(""))
                .AutoClear(false)
                .Overflow(VerticalOverflow.Ellipsis)
                .Start(ctx =>
                {
                    while (!token.IsCancellationRequested)
                    {
                        Thread.Sleep(200);
                        ctx.UpdateTarget(RenderDisplay());
                    }
                });
        }
        catch (OperationCanceledException)
        {
        }

        channel.Writer.TryComplete();

        try
        {
            streamTask.Wait(TimeSpan.FromSeconds(5));
        }
        catch
        {
        }

        Console.Clear();
        RenderFinalSummary();

        return 0;
    }

    private void RenderFinalSummary()
    {
        var files = _completedFiles.ToArray();
        var successCount = files.Count(f => f.IsSuccess);
        var failCount = files.Length - successCount;

        AnsiConsole.MarkupLine("[bold blue]TuneFlow Watch[/] - 处理完成\n");

        var summary = new Panel(
                new Markup($"[green]成功: {successCount}[/]  [red]失败: {failCount}[/]  [dim]总计: {files.Length}[/]"))
            .Border(BoxBorder.Rounded)
            .BorderColor(Color.Blue);

        AnsiConsole.Write(summary);
        AnsiConsole.WriteLine();

        if (files.Length == 0)
        {
            AnsiConsole.MarkupLine("[dim]未处理任何文件[/]");
            return;
        }

        var table = new Table()
            .Border(TableBorder.Rounded)
            .BorderColor(Color.Grey)
            .AddColumn("状态")
            .AddColumn("源文件")
            .AddColumn("结果");

        foreach (var file in files)
            if (file.IsSuccess)
                table.AddRow(
                    "[green]√[/]",
                    $"[white]{file.SourceFile.EscapeMarkup()}[/]",
                    $"[green]{file.OutputFile.EscapeMarkup()}[/]");
            else
                table.AddRow(
                    "[red]×[/]",
                    $"[white]{file.SourceFile.EscapeMarkup()}[/]",
                    $"[red]{file.ErrorMessage.EscapeMarkup()}[/]");

        AnsiConsole.Write(table);
    }

    private Rows RenderDisplay()
    {
        var rows = new List<IRenderable>();

        var progressPanel = RenderProgressPanel();
        rows.Add(progressPanel);

        var completedPanel = RenderCompletedPanel();
        rows.Add(completedPanel);

        return new Rows(rows);
    }

    private Panel RenderProgressPanel()
    {
        if (_progressMap.IsEmpty)
            return new Panel(new Markup("[dim]等待新文件...[/]"))
                .Header("[bold cyan]处理中[/]")
                .Border(BoxBorder.Rounded)
                .BorderColor(Color.Grey);

        var table = new Table()
            .Border(TableBorder.None)
            .AddColumn("文件")
            .AddColumn("阶段")
            .AddColumn("耗时");

        foreach (var kvp in _progressMap.OrderBy(x => x.Value.StartTime))
        {
            var progress = kvp.Value;
            var fileName = Path.GetFileName(progress.FilePath);
            var stage = GetStageDisplay(progress.Stage);
            var elapsed = progress.TotalElapsed.ToString(@"mm\:ss\.ff");

            table.AddRow(
                $"[white]{fileName.EscapeMarkup()}[/]",
                stage,
                $"[dim]{elapsed}[/]");
        }

        return new Panel(table)
            .Header("[bold cyan]处理中[/]")
            .Border(BoxBorder.Rounded)
            .BorderColor(Color.Cyan);
    }

    private Panel RenderCompletedPanel()
    {
        var files = _completedFiles.ToArray();
        if (files.Length == 0)
            return new Panel(new Markup("[dim]暂无已完成文件[/]"))
                .Header("[bold green]已完成[/]")
                .Border(BoxBorder.Rounded)
                .BorderColor(Color.Grey);

        var table = new Table()
            .Border(TableBorder.None)
            .AddColumn("状态")
            .AddColumn("源文件")
            .AddColumn("结果");

        var displayFiles = files.TakeLast(10).ToArray();
        foreach (var file in displayFiles)
            if (file.IsSuccess)
                table.AddRow(
                    "[green]√[/]",
                    $"[white]{file.SourceFile.EscapeMarkup()}[/]",
                    $"[green]{file.OutputFile.EscapeMarkup()}[/]");
            else
                table.AddRow(
                    "[red]×[/]",
                    $"[white]{file.SourceFile.EscapeMarkup()}[/]",
                    $"[red]{file.ErrorMessage.EscapeMarkup()}[/]");

        var header = files.Length > 10
            ? $"[bold green]已完成[/] [dim]({files.Length})[/]"
            : "[bold green]已完成[/]";

        return new Panel(table)
            .Header(header)
            .Border(BoxBorder.Rounded)
            .BorderColor(Color.Green);
    }

    private static string GetStageDisplay(WorkflowStage stage)
    {
        return stage switch
        {
            WorkflowStage.Started => "[yellow]开始处理[/]",
            WorkflowStage.Decrypted => "[blue]解密中[/]",
            WorkflowStage.GotLyrics => "[magenta]获取歌词[/]",
            WorkflowStage.GotCover => "[cyan]获取封面[/]",
            WorkflowStage.EmbeddedInfo => "[green]嵌入信息[/]",
            WorkflowStage.SavedToFile => "[blue]保存文件[/]",
            WorkflowStage.Finished => "[green]完成[/]",
            _ => $"[dim]{stage}[/]"
        };
    }

    private async Task DebounceAndEnqueueAsync(
        string path,
        ChannelWriter<WorkflowRequest> writer,
        IProgress<WorkflowProgress> progress,
        CancellationToken ct)
    {
        try
        {
            await Task.Delay(500, ct);

            if (!_ctsMap.TryRemove(path, out _))
                return;

            var ext = Path.GetExtension(path).ToLowerInvariant();
            if (ext is not ".ncm")
            {
                AnsiConsole.MarkupLine($"[yellow]暂不支持处理:[/] {Path.GetFileName(path)} [dim]({ext})[/]");
                return;
            }

            if (!await WaitForFileReadyAsync(path, ct))
            {
                AnsiConsole.MarkupLine($"[yellow]文件仍被占用，跳过:[/] {Path.GetFileName(path)}");
                return;
            }

            ArgumentNullException.ThrowIfNull(_settings);

            var request = new WorkflowRequest
            {
                SourceFilePath = path,
                OutputDirectory = _settings.SavePath,
                LyricsOptions = new LyricsOptions
                {
                    Embed = _settings.EmbedLyrics,
                    SaveToFile = _settings.SaveLyrics,
                    SavePath = _settings.LyricsPath
                },
                CoverOptions = new CoverOptions
                {
                    Embed = _settings.EmbedCover,
                    SaveToFile = _settings.SaveCover,
                    SavePath = _settings.CoverPath
                },
                Progress = progress
            };

            await writer.WriteAsync(request, ct);
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            AnsiConsole.WriteException(ex);
        }
    }

    private async Task<bool> WaitForFileReadyAsync(string path, CancellationToken token, int maxRetry = 10,
        int delayMs = 200)
    {
        for (var i = 0; i < maxRetry; i++)
        {
            token.ThrowIfCancellationRequested();

            if (IsFileReady(path))
                return true;

            await Task.Delay(delayMs, token);
        }

        return false;
    }

    private static bool IsFileReady(string path)
    {
        try
        {
            using var stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.None);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private class FileProgress(WorkflowProgress progress)
    {
        public string FilePath { get; } = progress.File.FullName;
        public WorkflowStage Stage { get; private set; } = progress.Stage;
        public TimeSpan TotalElapsed { get; private set; } = progress.TotalElapsed;
        public DateTime StartTime { get; } = DateTime.Now;

        public void Update(WorkflowProgress progress)
        {
            Stage = progress.Stage;
            TotalElapsed = progress.TotalElapsed;
        }
    }

    private record CompletedFile(string SourceFile, string? OutputFile = null, string? ErrorMessage = null)
    {
        public bool IsSuccess => ErrorMessage is null;
    }

    public class Settings : CommandSettings
    {
        [CommandArgument(0, "<path>")]
        [Description("监视路径")]
        public required string Path { get; set; }

        [CommandOption("-s|--save-path")]
        [Description("结果保存路径")]
        public required string SavePath { get; set; }

        [CommandOption("--embed-lyrics")]
        [DefaultValue(true)]
        public bool EmbedLyrics { get; set; }

        [CommandOption("--embed-cover")]
        [DefaultValue(true)]
        public bool EmbedCover { get; set; }

        [CommandOption("--save-lyrics")]
        [DefaultValue(false)]
        public bool SaveLyrics { get; set; }

        [CommandOption("--save-cover")]
        [DefaultValue(false)]
        public bool SaveCover { get; set; }

        [CommandOption("--lyrics-path")] public string? LyricsPath { get; set; }

        [CommandOption("--cover-path")] public string? CoverPath { get; set; }
    }
}