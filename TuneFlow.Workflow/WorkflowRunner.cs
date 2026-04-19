using System.Threading.Channels;
using NcmFox;
using NcmFox.Models;
using TagLib;
using TagLib.Id3v2;
using TuneFlow.Workflow.Blocks;
using TuneFlow.Workflow.Options;
using File = TagLib.File;

namespace TuneFlow.Workflow;

public class WorkflowRunner(GetLyricsBlock getLyricsBlock, GetCoverBlock getCoverBlock)
{
    public async Task<WorkflowResult> RunAsync(WorkflowRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        try
        {
            var context = CreateContext(request);
            await ExecutePipelineAsync(context, ct);

            return WorkflowResult.Success(
                request.SourceFilePath,
                context.OutputPath,
                context.LyricsFilePath,
                context.CoverFilePath);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return WorkflowResult.Failure(request.SourceFilePath, ex);
        }
    }

    public async Task<IReadOnlyList<WorkflowResult>> RunBatchAsync(
        IEnumerable<WorkflowRequest> requests,
        BatchOptions? options = null,
        CancellationToken ct = default)
    {
        options ??= new BatchOptions();

        var channel = Channel.CreateBounded<WorkflowRequest>(
            new BoundedChannelOptions(options.BoundedCapacity)
            {
                SingleReader = false,
                SingleWriter = true
            });

        var results = new List<WorkflowResult>();
        var lockObj = new object();

        var producerTask = ProduceAsync(channel.Writer, requests, ct);

        await Parallel.ForEachAsync(
            channel.Reader.ReadAllAsync(ct),
            new ParallelOptions
            {
                MaxDegreeOfParallelism = options.MaxDegreeOfParallelism,
                CancellationToken = ct
            },
            async (request, token) =>
            {
                var result = await RunAsync(request, token);
                lock (lockObj)
                {
                    results.Add(result);
                }
            });

        await producerTask;

        return results;
    }

    public async Task RunStreamAsync(
        ChannelReader<WorkflowRequest> reader,
        Action<WorkflowResult>? onResult = null,
        int maxDegreeOfParallelism = 4,
        CancellationToken ct = default)
    {
        await Parallel.ForEachAsync(
            reader.ReadAllAsync(ct),
            new ParallelOptions
            {
                MaxDegreeOfParallelism = maxDegreeOfParallelism,
                CancellationToken = ct
            },
            async (request, token) =>
            {
                var result = await RunAsync(request, token);
                onResult?.Invoke(result);
            });
    }

    private static async Task ProduceAsync(
        ChannelWriter<WorkflowRequest> writer,
        IEnumerable<WorkflowRequest> requests,
        CancellationToken ct)
    {
        try
        {
            foreach (var request in requests)
            {
                ct.ThrowIfCancellationRequested();
                await writer.WriteAsync(request, ct);
            }
        }
        finally
        {
            writer.Complete();
        }
    }

    private static WorkflowContext CreateContext(WorkflowRequest request)
    {
        var ncmFile = NcmDecoder.Open(request.SourceFilePath);
        var meta = ncmFile.MetaData
                   ?? throw new InvalidOperationException("NCM metadata is missing.");

        Directory.CreateDirectory(request.OutputDirectory);

        var baseFileName = Path.GetFileNameWithoutExtension(ncmFile.FileInfo.Name);
        var extension = meta.SaveFormat switch
        {
            SaveFormat.Mp3 => ".mp3",
            SaveFormat.Flac => ".flac",
            _ => throw new NotSupportedException($"Unsupported format: {meta.SaveFormat}")
        };

        var outputPath = Path.Combine(request.OutputDirectory, baseFileName + extension);
        string? lyricsPath = null, coverPath = null;
        if (request.LyricsOptions.SaveToFile)
            lyricsPath = request.LyricsOptions.SavePath ?? Path.Combine(request.OutputDirectory, baseFileName + ".lrc");

        if (request.CoverOptions.SaveToFile)
        {
            if (request.CoverOptions.SavePath is not null)
            {
                coverPath = request.CoverOptions.SavePath;
            }
            else
            {
                var coverExtension = ncmFile.CoverData?.Format switch
                {
                    CoverFormat.Jpeg => ".jpg",
                    CoverFormat.Png => ".png",
                    _ => ".jpg"
                };
                coverPath = Path.Combine(request.OutputDirectory, baseFileName + coverExtension);
            }
        }

        return new WorkflowContext
        {
            File = ncmFile.FileInfo,
            NcmFile = ncmFile,
            OutputPath = outputPath,
            LyricsFilePath = lyricsPath,
            CoverFilePath = coverPath,
            LyricsOptions = request.LyricsOptions,
            CoverOptions = request.CoverOptions,
            Progress = request.Progress
        };
    }

    private async Task ExecutePipelineAsync(WorkflowContext context, CancellationToken ct)
    {
        if (!context.File.Exists)
            throw new FileNotFoundException("Input file not found.", context.File.FullName);

        context.ReportStage(WorkflowStage.Started);

        context.NcmFile.Decode(context.OutputPath);
        context.ReportStage(WorkflowStage.Decrypted);

        if (context.LyricsOptions.ShouldGet) await getLyricsBlock.ProcessAsync(context, ct);

        if (context.CoverOptions.ShouldGet) await getCoverBlock.ProcessAsync(context, ct);

        if (context.LyricsOptions.Embed || context.CoverOptions.Embed)
        {
            Embed(context);
            context.ReportStage(WorkflowStage.EmbeddedInfo);
        }

        if (context.LyricsOptions.SaveToFile || context.CoverOptions.SaveToFile)
        {
            await SaveToFileAsync(context, ct);
            context.ReportStage(WorkflowStage.SavedToFile);
        }

        context.ReportStage(WorkflowStage.Finished);
    }

    private static void Embed(WorkflowContext context)
    {
        using var file = File.Create(context.OutputPath);

        var meta = context.NcmFile.MetaData!;
        file.Tag.Title = meta.SongName;
        file.Tag.Album = meta.AlbumName;
        file.Tag.Performers = [string.Join('/', meta.GetArtists())];

        if (context.LyricsOptions.Embed && context.ExportedLyric is not null)
            file.Tag.Lyrics = context.ExportedLyric.Replace("\n", "\r\n");

        if (context.CoverOptions.Embed && context.CoverData is not null)
        {
            var mimeType = context.NcmFile.CoverData?.Format switch
            {
                CoverFormat.Jpeg => "image/jpeg",
                CoverFormat.Png => "image/png",
                _ => "image/jpeg"
            };

            var image = new AttachmentFrame
            {
                Data = context.CoverData,
                MimeType = mimeType,
                Type = PictureType.FrontCover,
                Description = "Cover (front)"
            };
            file.Tag.Pictures = [image];
        }

        file.Save();
    }

    private static async Task SaveToFileAsync(WorkflowContext context, CancellationToken ct)
    {
        if (context.LyricsOptions.SaveToFile && context.ExportedLyric is not null && context.LyricsFilePath is not null)
            await System.IO.File.WriteAllTextAsync(context.LyricsFilePath, context.ExportedLyric, ct);

        if (context.CoverOptions.SaveToFile && context.CoverData is not null && context.CoverFilePath is not null)
            await System.IO.File.WriteAllBytesAsync(context.CoverFilePath, context.CoverData, ct);
    }
}