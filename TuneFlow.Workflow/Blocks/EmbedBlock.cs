using NcmFox;
using NcmFox.Models;
using TagLib;
using TuneFlow.Lyrics.Exporting;
using TuneFlow.Workflow.Options;
using File = TagLib.File;

namespace TuneFlow.Workflow.Blocks;

public class EmbedBlock
{
    public Task ProcessAsync(WorkflowContext context, CancellationToken ct = default)
    {
        using var file = File.Create(context.OutputPath);

        var meta = context.NcmFile.MetaData;
        ArgumentNullException.ThrowIfNull(meta);
        file.Tag.Title = meta.SongName;
        file.Tag.Album = meta.AlbumName;
        file.Tag.Performers = [string.Join('/', meta.GetArtists())];

        if (context.LyricsOptions.Embed)
        {
            ArgumentException.ThrowIfNullOrEmpty(context.ExportedLyric);
            var lyrics = context.ExportedLyric.Replace("\n", "\r\n");
            file.Tag.Lyrics = lyrics;
        }

        if (context.CoverOptions.Embed)
        {
            ArgumentNullException.ThrowIfNull(context.CoverData);
            var mimeType = ResolveCoverMimeType(context);
            var image = new TagLib.Id3v2.AttachmentFrame
            {
                Data = context.CoverData,
                MimeType = mimeType,
                Type = PictureType.FrontCover,
                Description = "Cover (front)"
            };
            file.Tag.Pictures = [image];
        }
        
        file.Save();
        context.ReportStage(WorkflowStage.EmbeddedInfo);
        
        return Task.CompletedTask;
    }

    private static string ResolveCoverMimeType(WorkflowContext context)
    {
        // Prefer the declared NCM cover format when available.
        return context.NcmFile.CoverData?.Format switch
        {
            CoverFormat.Jpeg => "image/jpeg",
            CoverFormat.Png => "image/png",
            _ => "image/jpeg"
        };
    }
}
