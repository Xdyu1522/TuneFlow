using NcmFox.Models;

namespace TuneFlow.Workflow.Extensions;

public static class NcmFileExtensions
{
    extension(NcmFile ncm)
    {
        public string? LyricUrl =>
            ncm.MetaData?.Id is { } id
                ? $"https://music.163.com/api/song/lyric?os=pc&id={id}&lv=-1&tv=-1&rv=-1"
                : null;
        
        public string? CoverName =>
            ncm.CoverData is { } coverData
                ? Path.ChangeExtension(ncm.FileInfo.Name, GetExtension(coverData.Format))
                : null;

        public string LyricName => Path.ChangeExtension(ncm.FileInfo.Name, ".lrc");
    }
    private static string GetExtension(CoverFormat format) => format switch
    {
        CoverFormat.Jpeg => ".jpg",
        CoverFormat.Png  => ".png",
        _ => ".img"
    };
}