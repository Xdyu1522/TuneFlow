namespace TuneFlow.Workflow.Providers;

public static class CoverDownloader
{
    public static async Task<byte[]?> DownloadCover(HttpClient client, string url, CancellationToken ct = default)
    {
        try
        {
            using var response = await client.GetAsync(url, ct);
            if (!response.IsSuccessStatusCode) return null;
            return await response.Content.ReadAsByteArrayAsync(ct);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception)
        {
            return null;
        }
    }
}