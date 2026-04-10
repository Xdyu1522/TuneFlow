# TuneFlow.Workflow

`TuneFlow.Workflow` 提供 NCM 处理流水线，负责：

- 读取与校验输入文件
- 解密音频
- 拉取并处理歌词
- 拉取封面
- 嵌入元信息（歌词/封面）
- 按配置保存附加资源文件

当前实现基于 `TPL Dataflow`，并通过 `WorkflowFactory` 按上下文配置动态连接 Block。

## 核心结构

### 1) 上下文与请求模型

- `WorkflowRequest`
  - 面向“外部调用输入”
  - 支持两种输出定位方式：
    - `OutputPath`（完整路径）
    - `OutputDirectory + OutputFileName`（扩展名由 `NcmFile.SaveFormat` 自动推断）
- `WorkflowContext`
  - 面向“内部流水线状态”
  - 包含：
    - 输入文件与 `NcmFile`
    - 输出路径
    - `LyricsOptions` / `CoverOptions`
    - `WorkflowExecutionOptions`（Dataflow 执行参数）
    - 运行时数据：`CoverData`、`LyricsDocument`、`ExportedLyric`
    - `IProgress<WorkflowProgress>`
- `WorkflowContextBuilder`
  - Fluent 构建器
  - 用于显式构造 `WorkflowContext`

### 2) Block 列表

- `PrepareContextBlock`（请求入口专用）
  - `WorkflowRequest -> WorkflowContext`
  - 内部调用 `NcmDecoder.Open(...)`
- `StartBlock`
  - 输入合法性校验与目录准备
- `DecryptBlock`
  - 解密输出音频
- `GetLyricsBlock`
  - 按 `LyricsOptions.Strategy` 选择 provider 拉取并导出歌词字符串
- `GetCoverBlock`
  - 按 `CoverOptions.Strategy` 选择 provider 拉取封面，支持 `NetWorkFirst`/`InFileFirst` fallback
- `EmbedBlock`
  - 写入标题、艺人、专辑、歌词、封面
- `SaveToFileBlock`
  - 保存歌词/封面到文件

### 3) 工厂与运行器

- `WorkflowFactory`
  - 根据 `WorkflowContext` 动态拼装 pipeline
  - 控制 block 连接顺序与条件连接
- `WorkflowRunner`
  - 对外执行入口
  - 支持三种调用方式：
    - `RunAsync(WorkflowContext context)`
    - `RunAsync(Action<WorkflowContextBuilder> configure)`
    - `RunAsync(WorkflowRequest request)`（仅传路径与配置）

## Dataflow 连接规则

固定顺序：

1. `Start`
2. `Decrypt`

条件步骤：

- `LyricsOptions.ShouldGet == true` 时接 `GetLyrics`
- `CoverOptions.ShouldGet == true` 时接 `GetCover`
- `LyricsOptions.Embed || CoverOptions.Embed` 时接 `Embed`
- `LyricsOptions.SaveToFile || CoverOptions.SaveToFile` 时接 `SaveToFile`

最后统一触发 `WorkflowStage.Finished`。

## 执行配置（TPL Dataflow）

`WorkflowExecutionOptions` 当前支持：

- `MaxDegreeOfParallelism`
- `EnsureOrdered`
- `BoundedCapacity`
- `MaxMessagesPerTask`

配置入口：

- `WorkflowRequest.ExecutionOptions`
- `WorkflowContextBuilder.UseExecutionOptions(...)`
- `WorkflowContextBuilder.ConfigureExecution(...)`

说明：

- 单条 workflow 内部是按链路顺序执行的
- 多文件并行建议并发调用 `runner.RunAsync(...)`（每文件一条 workflow）

## 配置模型说明

### LyricsOptions

- `Embed` / `SaveToFile` / `SavePath`
- `Strategy`（歌词来源策略）
- `ExportFormat` / `ExportMode` / `LineBreak`
- `IncludeKinds`（翻译、罗马音等轨道）
- `MaxTimeDeltaMs`（多轨 merge 时间容差）

### CoverOptions

- `Embed` / `SaveToFile` / `SavePath`
- `Strategy`
  - `NetWork`
  - `InFile`
  - `NetWorkFirst`
  - `InFileFirst`

## 进度回调

通过 `IProgress<WorkflowProgress>` 接收阶段进度：

- `Started`
- `Decrypted`
- `GotLyrics`
- `GotCover`
- `EmbeddedInfo`
- `SavedToFile`
- `Finished`

## 使用方式

### 1) 仅传路径和配置（推荐）

```csharp
await runner.RunAsync(new WorkflowRequest
{
    SourceFilePath = @"D:\music\song.ncm",
    OutputDirectory = @"D:\music\out",
    OutputFileName = "song",
    LyricsOptions = new LyricsOptions
    {
        Embed = true,
        SaveToFile = true,
        SavePath = @"D:\music\out\song.lrc"
    },
    CoverOptions = new CoverOptions
    {
        Embed = true,
        SaveToFile = true,
        SavePath = @"D:\music\out\song.jpg",
        Strategy = CoverSourceStrategy.NetWorkFirst
    },
    ExecutionOptions = new WorkflowExecutionOptions
    {
        MaxDegreeOfParallelism = 1,
        EnsureOrdered = true
    },
    Progress = new Progress<WorkflowProgress>(p =>
        Console.WriteLine($"{p.Stage} | step={p.StepElapsed} | total={p.TotalElapsed}"))
});
```

### 2) Fluent Builder 入口

```csharp
await runner.RunAsync(builder => builder
    .FromNcmFile(ncmFile)
    .ToOutput(@"D:\music\out", "song")
    .ConfigureLyrics(o => o with { Embed = true, SaveToFile = true, SavePath = @"D:\music\out\song.lrc" })
    .ConfigureCover(o => o with { Embed = true, SaveToFile = true, SavePath = @"D:\music\out\song.jpg" })
    .ConfigureExecution(o => o with { MaxDegreeOfParallelism = 1 }));
```

### 3) 直接传上下文

```csharp
var context = WorkflowFactory.CreateBuilder()
    .FromNcmFile(ncmFile)
    .ToOutput(@"D:\music\out\song.flac")
    .Build();

await runner.RunAsync(context);
```

## DI 注册

```csharp
var services = new ServiceCollection();
services.AddWorkflow();
```

`AddWorkflow()` 会注册：

- 所有 workflow blocks
- `WorkflowFactory`
- `WorkflowRunner`
- 封面与歌词 provider（含 HttpClient）

## 扩展建议

后续可扩展方向：

- 在 `WorkflowFactory` 中引入可配置 block 插拔机制
- 为批量任务增加上层队列与并发限制器
- 为 `WorkflowRequest` 增加统一输出模板（歌词/封面默认命名规则）
- 为 `WorkflowProgress` 增加文件级别 trace id 与错误事件
