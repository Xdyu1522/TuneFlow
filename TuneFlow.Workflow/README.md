# TuneFlow.Workflow

`TuneFlow.Workflow` 提供 NCM 文件处理流水线，负责：

- 解密音频文件
- 拉取并处理歌词
- 拉取封面图片
- 嵌入元信息（歌词/封面）
- 保存附加资源文件

基于 `Channel` + `Parallel.ForEachAsync` 实现高效的文件级并行处理与背压控制。

## 核心结构

### 1) 请求与结果模型

- `WorkflowRequest` - 输入请求
  - `SourceFilePath`：源文件路径（必需）
  - `OutputDirectory`：输出目录（必需，文件名自动从 NCM 元数据获取）
  - `LyricsOptions`：歌词处理配置
  - `CoverOptions`：封面处理配置
  - `Progress`：进度回调

- `WorkflowResult` - 处理结果
  - `SourceFilePath`：源文件路径
  - `OutputFilePath`：输出音频文件路径
  - `LyricsFilePath`：歌词文件路径（如保存）
  - `CoverFilePath`：封面文件路径（如保存）
  - `Error`：错误信息（失败时）
  - `IsSuccess`：是否成功

- `WorkflowContext` - 内部流水线状态
  - 包含运行时数据：`CoverData`、`LyricsDocument`、`ExportedLyric`

### 2) Block 列表

- `GetLyricsBlock` - 按 `LyricsOptions.Strategy` 选择 provider 拉取并导出歌词
- `GetCoverBlock` - 按 `CoverOptions.Strategy` 选择 provider 拉取封面，支持 fallback

### 3) 运行器

- `WorkflowRunner` - 对外执行入口
  - `RunAsync`：单文件处理，返回 `WorkflowResult`
  - `RunBatchAsync`：批量处理，返回 `IReadOnlyList<WorkflowResult>`
  - `RunStreamAsync`：流式处理，支持外部 Channel 输入

## 文件命名规则

输出文件名与原始 NCM 文件名保持一致：

- 音频文件：`{原始文件名}.{mp3|flac}`
- 歌词文件：`{原始文件名}.lrc`
- 封面文件：`{原始文件名}.{jpg|png}`（根据封面格式自动确定）

## 配置模型

### LyricsOptions

```csharp
public record LyricsOptions
{
    public bool Embed { get; init; }           // 嵌入到音频文件
    public bool SaveToFile { get; init; }      // 保存为独立文件
    public LyricsSourceStrategy Strategy { get; init; }  // 来源策略
    
    // 导出配置
    public ExportFormat ExportFormat { get; init; }      // LRC / JSON
    public ExportMode ExportMode { get; init; }          // Interleaved / Separate
    public string LineBreak { get; init; }               // 换行符
    public ImmutableHashSet<LyricTrackKind> IncludeKinds { get; init; }  // 包含的轨道
}
```

### CoverOptions

```csharp
public record CoverOptions
{
    public bool Embed { get; init; }           // 嵌入到音频文件
    public bool SaveToFile { get; init; }      // 保存为独立文件
    public CoverSourceStrategy Strategy { get; init; }   // 来源策略
}
```

### BatchOptions

```csharp
public record BatchOptions
{
    public int MaxDegreeOfParallelism { get; init; }  // 最大并行度
    public int BoundedCapacity { get; init; }         // 队列容量（背压）
}
```

## 进度回调

通过 `IProgress<WorkflowProgress>` 接收阶段进度：

- `Started` → `Decrypted` → `GotLyrics` → `GotCover` → `EmbeddedInfo` → `SavedToFile` → `Finished`

## 使用方式

### 1) 单文件处理

```csharp
var runner = serviceProvider.GetRequiredService<WorkflowRunner>();

var result = await runner.RunAsync(new WorkflowRequest
{
    SourceFilePath = @"D:\music\song.ncm",
    OutputDirectory = @"D:\music\out",
    LyricsOptions = new LyricsOptions { Embed = true, SaveToFile = true },
    CoverOptions = new CoverOptions { Embed = true, SaveToFile = true },
    Progress = new Progress<WorkflowProgress>(p =>
        Console.WriteLine($"{p.Stage} | {p.TotalElapsed.TotalMilliseconds:F0}ms"))
});

if (result.IsSuccess)
{
    Console.WriteLine($"输出: {result.OutputFilePath}");
    Console.WriteLine($"歌词: {result.LyricsFilePath}");
    Console.WriteLine($"封面: {result.CoverFilePath}");
}
else
{
    Console.WriteLine($"失败: {result.Error!.Message}");
}
```

### 2) 批量处理

```csharp
var files = Directory.GetFiles(@"D:\music", "*.ncm");

var requests = files.Select(f => new WorkflowRequest
{
    SourceFilePath = f,
    OutputDirectory = @"D:\music\out",
    LyricsOptions = new LyricsOptions { Embed = true },
    CoverOptions = new CoverOptions { Embed = true }
});

var results = await runner.RunBatchAsync(requests, new BatchOptions
{
    MaxDegreeOfParallelism = 8,
    BoundedCapacity = 50
});

var successes = results.Where(r => r.IsSuccess);
var failures = results.Where(r => !r.IsSuccess);

Console.WriteLine($"成功: {successes.Count()}, 失败: {failures.Count()}");

foreach (var f in failures)
{
    Console.WriteLine($"  {Path.GetFileName(f.SourceFilePath)}: {f.Error!.Message}");
}
```

### 3) 流式处理

适用于文件监视器、消息队列等持续数据源：

```csharp
var channel = Channel.CreateBounded<WorkflowRequest>(100);

// 生产者（如文件监视器）
var watcher = new FileSystemWatcher(@"D:\incoming", "*.ncm");
watcher.Created += async (s, e) =>
{
    await channel.Writer.WriteAsync(new WorkflowRequest
    {
        SourceFilePath = e.FullPath,
        OutputDirectory = @"D:\music\out"
    });
};
watcher.EnableRaisingEvents = true;

// 消费者（后台持续处理）
_ = runner.RunStreamAsync(channel.Reader, maxDegreeOfParallelism: 4);
```

### 4) 根据结果重命名文件

```csharp
var result = await runner.RunAsync(request);

if (result.IsSuccess)
{
    // 重命名音频文件
    var newName = $"[{result.OutputFilePath}]";
    File.Move(result.OutputFilePath, Path.Combine(Path.GetDirectoryName(result.OutputFilePath)!, newName));
    
    // 或移动到其他目录
    File.Move(result.OutputFilePath, @"D:\organized\music\song.mp3");
}
```

## DI 注册

```csharp
var services = new ServiceCollection();
services.AddWorkflow();
```

`AddWorkflow()` 会注册：

- `WorkflowRunner`
- `GetLyricsBlock`、`GetCoverBlock`
- 封面与歌词 provider（含 HttpClient）
