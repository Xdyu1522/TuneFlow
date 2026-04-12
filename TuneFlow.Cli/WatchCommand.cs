using System.Collections.Concurrent;
using System.ComponentModel;
using Spectre.Console;
using Spectre.Console.Cli;
using TuneFlow.Workflow;
using TuneFlow.Workflow.Options;

namespace TuneFlow.Cli;

public class WatchCommand(WorkflowRunner runner) : Command<WatchCommand.Settings>
{
    private readonly ConcurrentDictionary<string, CancellationTokenSource> _ctsMap = new();
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

            // 1. 取消旧任务
            if (_ctsMap.TryRemove(path, out var oldCts))
            {
                oldCts.Cancel();
                oldCts.Dispose();
            }

            // 2. 创建新的 CTS（使用原子更新更安全）
            var cts = new CancellationTokenSource();
            _ctsMap.AddOrUpdate(path, cts, (_, __) => cts);

            // 3. 启动防抖任务（无需 Task.Run）
            _ = DebounceAsync(path, cts);
        };

        AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .Start("正在监视文件更改... 按 [bold]Ctrl+C[/] 停止", ctx =>
            {
                // 卡住线程，直到用户取消任务
                cancellationToken.WaitHandle.WaitOne();
            });

        return 0;
    }

    private async Task OnDownloadCompletedAsync(object? state)
    {
        ArgumentNullException.ThrowIfNull(_settings);
        var filePath = (string)state!;

        // 此时文件大概率已经下载完毕且解开了锁定
        AnsiConsole.MarkupLine($"[bold green]下载完成:[/] {Path.GetFileName(filePath)}，准备开始解密...");
        var result = await runner.RunAsync(new WorkflowRequest
        {
            SourceFilePath = filePath,
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
            }
        });
        if (result.IsSuccess) AnsiConsole.MarkupLine("[bold green]解密完成[/]");
        else
            AnsiConsole.MarkupLine($"[bold red]{result.Error}[/]");
    }

    private async Task DebounceAsync(string path, CancellationTokenSource cts)
    {
        try
        {
            // 防抖等待
            await Task.Delay(500, cts.Token);

            // 再次确认是最后一个任务
            if (!_ctsMap.TryRemove(path, out _))
                return;

            // ⚠️ 文件可能还没写完，做“就绪检测”
            if (!await WaitForFileReadyAsync(path, cts.Token))
            {
                AnsiConsole.MarkupLine($"[yellow]文件仍被占用，跳过:[/] {Path.GetFileName(path)}");
                return;
            }

            // 真正执行
            await OnDownloadCompletedAsync(path);
        }
        catch (OperationCanceledException)
        {
            // 正常取消，无需处理
        }
        catch (Exception ex)
        {
            AnsiConsole.WriteException(ex);
        }
        finally
        {
            cts.Dispose(); // 防止泄漏
        }
    }

    private async Task<bool> WaitForFileReadyAsync(
        string path,
        CancellationToken token,
        int maxRetry = 10,
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

    private bool IsFileReady(string path)
    {
        try
        {
            using var stream = File.Open(
                path,
                FileMode.Open,
                FileAccess.Read,
                FileShare.None); // 独占访问

            return true;
        }
        catch
        {
            return false;
        }
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