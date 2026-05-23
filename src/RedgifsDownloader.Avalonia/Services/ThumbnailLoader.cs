using System.IO;
using Avalonia.Media.Imaging;
using RedgifsDownloader.Avalonia.ViewModels;

namespace RedgifsDownloader.Avalonia.Services;

public sealed class ThumbnailLoader
{
    private readonly HttpClient _httpClient;

    public ThumbnailLoader(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task LoadAsync(VideoRow row)
    {
        if (string.IsNullOrWhiteSpace(row.ThumbnailUrl))
        {
            row.ThumbnailImage = null;
            return;
        }

        try
        {
            await using var stream = await _httpClient.GetStreamAsync(row.ThumbnailUrl);
            using var copy = new MemoryStream();
            await stream.CopyToAsync(copy);
            copy.Position = 0;
            row.ThumbnailImage = new Bitmap(copy);
        }
        catch
        {
            row.ThumbnailImage = null;
        }
    }
}
