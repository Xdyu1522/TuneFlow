using NcmFox.Models;
using TuneFlow.Workflow.Options;

namespace TuneFlow.Workflow;

public sealed class WorkflowContextBuilder
{
    private FileInfo? _file;
    private NcmFile? _ncmFile;
    private string? _outputPath;
    private string? _outputDirectory;
    private string? _outputFileName;
    private LyricsOptions _lyricsOptions = new();
    private CoverOptions _coverOptions = new();
    private WorkflowExecutionOptions _executionOptions = new();
    private IProgress<WorkflowProgress>? _progress;

    private WorkflowContextBuilder()
    {
    }

    public static WorkflowContextBuilder Create() => new();

    public WorkflowContextBuilder FromFile(FileInfo file)
    {
        _file = file ?? throw new ArgumentNullException(nameof(file));
        return this;
    }

    public WorkflowContextBuilder FromFile(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("Input file path cannot be empty.", nameof(filePath));
        }

        _file = new FileInfo(filePath);
        return this;
    }

    public WorkflowContextBuilder UseNcmFile(NcmFile ncmFile)
    {
        _ncmFile = ncmFile ?? throw new ArgumentNullException(nameof(ncmFile));
        return this;
    }

    public WorkflowContextBuilder FromNcmFile(NcmFile ncmFile)
    {
        ArgumentNullException.ThrowIfNull(ncmFile);
        _ncmFile = ncmFile;
        _file = ncmFile.FileInfo;
        return this;
    }

    public WorkflowContextBuilder ToOutput(string outputPath)
    {
        if (string.IsNullOrWhiteSpace(outputPath))
        {
            throw new ArgumentException("Output path cannot be empty.", nameof(outputPath));
        }

        _outputPath = outputPath;
        _outputDirectory = null;
        _outputFileName = null;
        return this;
    }

    public WorkflowContextBuilder ToOutput(string outputDirectory, string outputFileNameWithoutExtension)
    {
        if (string.IsNullOrWhiteSpace(outputDirectory))
        {
            throw new ArgumentException("Output directory cannot be empty.", nameof(outputDirectory));
        }

        if (string.IsNullOrWhiteSpace(outputFileNameWithoutExtension))
        {
            throw new ArgumentException("Output file name cannot be empty.", nameof(outputFileNameWithoutExtension));
        }

        _outputDirectory = outputDirectory;
        _outputFileName = outputFileNameWithoutExtension;
        _outputPath = null;
        return this;
    }

    public WorkflowContextBuilder UseLyricsOptions(LyricsOptions options)
    {
        _lyricsOptions = options ?? throw new ArgumentNullException(nameof(options));
        return this;
    }

    public WorkflowContextBuilder ConfigureLyrics(Func<LyricsOptions, LyricsOptions> configure)
    {
        if (configure is null) throw new ArgumentNullException(nameof(configure));
        _lyricsOptions = configure(_lyricsOptions) ?? throw new InvalidOperationException("Lyrics options cannot be null.");
        return this;
    }

    public WorkflowContextBuilder UseCoverOptions(CoverOptions options)
    {
        _coverOptions = options ?? throw new ArgumentNullException(nameof(options));
        return this;
    }

    public WorkflowContextBuilder ConfigureCover(Func<CoverOptions, CoverOptions> configure)
    {
        if (configure is null) throw new ArgumentNullException(nameof(configure));
        _coverOptions = configure(_coverOptions) ?? throw new InvalidOperationException("Cover options cannot be null.");
        return this;
    }

    public WorkflowContextBuilder WithProgress(IProgress<WorkflowProgress>? progress)
    {
        _progress = progress;
        return this;
    }

    public WorkflowContextBuilder UseExecutionOptions(WorkflowExecutionOptions options)
    {
        _executionOptions = options ?? throw new ArgumentNullException(nameof(options));
        return this;
    }

    public WorkflowContextBuilder ConfigureExecution(Func<WorkflowExecutionOptions, WorkflowExecutionOptions> configure)
    {
        if (configure is null) throw new ArgumentNullException(nameof(configure));
        _executionOptions = configure(_executionOptions) ?? throw new InvalidOperationException("Execution options cannot be null.");
        return this;
    }

    public WorkflowContext Build()
    {
        if (_file is null)
        {
            throw new InvalidOperationException("Input file is required. Call FromFile(...) first.");
        }

        if (_ncmFile is null)
        {
            throw new InvalidOperationException("NcmFile is required. Call UseNcmFile(...) first.");
        }

        var outputPath = ResolveOutputPath();
        if (string.IsNullOrWhiteSpace(outputPath))
        {
            throw new InvalidOperationException(
                "Output path is required. Call ToOutput(path) or ToOutput(directory, fileNameWithoutExtension) first.");
        }

        return new WorkflowContext
        {
            File = _file,
            NcmFile = _ncmFile,
            OutputPath = outputPath,
            LyricsOptions = _lyricsOptions,
            CoverOptions = _coverOptions,
            ExecutionOptions = _executionOptions,
            Progress = _progress
        };
    }

    private string? ResolveOutputPath()
    {
        if (!string.IsNullOrWhiteSpace(_outputPath))
        {
            return _outputPath;
        }

        if (string.IsNullOrWhiteSpace(_outputDirectory) || string.IsNullOrWhiteSpace(_outputFileName))
        {
            return null;
        }

        if (_ncmFile is null)
        {
            throw new InvalidOperationException("NcmFile is required before resolving output extension.");
        }

        var extension = _ncmFile.SaveFormat switch
        {
            SaveFormat.Flac => ".flac",
            SaveFormat.Mp3 => ".mp3",
            _ => throw new InvalidOperationException(
                "Cannot infer output extension because NcmFile.SaveFormat is unknown. Use ToOutput(path) explicitly.")
        };

        var safeName = Path.GetFileNameWithoutExtension(_outputFileName);
        return Path.Combine(_outputDirectory, safeName + extension);
    }
}
