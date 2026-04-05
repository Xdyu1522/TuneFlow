namespace TuneFlow.Lyrics.Tests;

internal static class TestResourceLocator
{
    private static readonly string ResourceRoot = Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "TestResources"));

    public static string GetPath(string fileName) => Path.Combine(ResourceRoot, fileName);

    public static string ReadAllText(string fileName) => File.ReadAllText(GetPath(fileName));
}
