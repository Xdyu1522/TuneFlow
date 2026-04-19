using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Serialization;
using NcmFox;
using NcmFox.Models;
using Spectre.Console;
using Spectre.Console.Cli;
using Spectre.Console.Json;
using Spectre.Console.Rendering;

namespace TuneFlow.Cli;

public class InfoCommand : Command<InfoCommandSettings>
{
    protected override int Execute(CommandContext context, InfoCommandSettings settings,
        CancellationToken cancellationToken)
    {
        if (!File.Exists(settings.File))
        {
            AnsiConsole.MarkupLine($"[red]文件不存在: {settings.File}[/]");
            return -1;
        }

        var ncmFile = NcmDecoder.Open(settings.File);
        var meta = ncmFile.MetaData;

        if (meta is null)
        {
            AnsiConsole.MarkupLine("[red]无法读取文件元数据[/]");
            return -1;
        }

        if (settings.Short)
        {
            AnsiConsole.MarkupLine(
                $"[white]{meta.SongName}[/] [dim]-[/] [cyan]{string.Join(", ", meta.Artists.Select(a => a.Name))}[/]");
            return 0;
        }

        if (settings.Json)
        {
            var json = JsonSerializer.Serialize(meta, new JsonSerializerOptions
            {
                WriteIndented = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.Never,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
            var jsonText = new JsonText(json);
            AnsiConsole.Write(jsonText);
            return 0;
        }

        AnsiConsole.MarkupLine(EscapeMarkup(settings.File));

        var termWidth = AnsiConsole.Profile.Width;
        var leftWidth = termWidth / 3;
        var rightWidth = termWidth - leftWidth;

        IRenderable leftContent;
        if (ncmFile.CoverData?.Data is { } coverBytes)
        {
            var fileSizeStr = coverBytes.Length switch
            {
                >= 1024 * 1024 => $"{coverBytes.Length / 1024.0 / 1024.0:F2} MB",
                _ => $"{coverBytes.Length / 1024.0:F1} KB"
            };
            var img = new CanvasImage(coverBytes).MaxWidth(leftWidth);
            leftContent = new Rows(img, new Markup($"{fileSizeStr} - {ncmFile.CoverData.Format}"));
        }
        else
        {
            leftContent = new Markup($"[dim](无封面)[/]\n[dim]{meta.SaveFormat}[/]");
        }

        var leftPanel = new Panel(leftContent)
            .Border(BoxBorder.Rounded)
            .BorderColor(Color.SkyBlue1)
            .Header("专辑封面");

        var table = new Table().HideHeaders();
        table.Border(TableBorder.Simple);
        table.AddColumn("属性", col => col.RightAligned());
        table.AddColumn("值", col => col.LeftAligned());
        table.AddRow("歌曲名", meta.SongName);
        table.AddRow("艺术家", string.Join(" / ", meta.GetArtists()));
        table.AddRow("专辑", meta.AlbumName);
        table.AddRow("时长", meta.Duration?.ToString(@"mm\:ss") ?? "-");
        table.AddRow("比特率", meta.Bitrate.HasValue ? $"{meta.Bitrate.Value / 1000} kbps" : "-");
        table.AddRow("文件格式", GetSaveFormat(ncmFile.SaveFormat));
        table.AddEmptyRow();
        table.AddRow("歌曲 ID", meta.Id);
        table.AddRow("专辑 ID", meta.AlbumId);
        if (!string.IsNullOrEmpty(meta.MvId)) table.AddRow("MV ID", meta.MvId);
        if (meta.Alias.Count > 0)
        {
            table.AddEmptyRow();
            table.AddRow("别名", string.Join(", ", meta.Alias));
        }

        if (meta.TranslatedNames.Count > 0) table.AddRow("译名", string.Join(", ", meta.TranslatedNames));
        if (meta.Fee.HasValue) table.AddRow("费用类型", $"{meta.Fee.Value}({GetFeeDescription(meta.Fee.Value)})");
        if (meta.VolumeDelta.HasValue) table.AddRow("音量调整", $"{meta.VolumeDelta.Value:F1} dB");

        var rightPanel = new Panel(table)
            .Border(BoxBorder.Rounded)
            .BorderColor(Color.SkyBlue1)
            .Header("音乐信息");

        var grid = new Grid()
            .AddColumn(new GridColumn().Width(leftWidth))
            .AddColumn(new GridColumn().Width(rightWidth));

        grid.AddRow(leftPanel, rightPanel);

        AnsiConsole.Write(grid);

        return 0;
    }

    private static string EscapeMarkup(string text)
    {
        return text.Replace("[", "[[").Replace("]", "]]");
    }

    private static string GetSaveFormat(SaveFormat format)
    {
        return format switch
        {
            SaveFormat.Flac => "FLAC",
            SaveFormat.Mp3 => "MP3",
            _ => "UNKNOWN"
        };
    }

    private static string GetFeeDescription(int fee)
    {
        return fee switch
        {
            0 => "免费",
            1 => "VIP",
            4 => "购买专辑",
            8 => "免费低音质",
            _ => $"未知 ({fee})"
        };
    }

    private static string ExtractCover(InfoCommandSettings settings, NcmCover coverData)
    {
        var coverFileName = Path.ChangeExtension(settings.File, coverData.Format switch
        {
            CoverFormat.Jpeg => ".jpg",
            CoverFormat.Png => ".png",
            _ => ".jpg"
        });
        File.WriteAllBytes(coverFileName, coverData.Data);
        return coverFileName;
    }
}

public class InfoCommandSettings : CommandSettings
{
    [CommandArgument(0, "<file>")]
    [Description("文件路径")]
    public required string File { get; set; }

    [CommandOption("--short")]
    [Description("简短展示信息")]
    public bool Short { get; set; }

    [CommandOption("--json")]
    [Description("以Json形式展示MetadData")]
    public bool Json { get; set; }

    [CommandOption("--extract-cover")]
    [Description("提取封面")]
    public bool ExtractCover { get; set; }
}