using System.IO;
using Avalonia.Platform;
using Avalonia.Media.Imaging;
using RedgifsDownloader.Avalonia.ViewModels;

namespace RedgifsDownloader.Avalonia.Services;

public sealed class ThumbnailLoader
{
    private readonly HttpClient _httpClient;
    private readonly Bitmap? _fallbackBitmap;

    public ThumbnailLoader(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _fallbackBitmap = LoadFallbackBitmap();
    }

    public async Task LoadAsync(VideoRow row)
    {
        if (string.IsNullOrWhiteSpace(row.ThumbnailUrl))
        {
            row.ThumbnailImage = _fallbackBitmap;
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
            row.ThumbnailImage = _fallbackBitmap;
        }
    }

    private static Bitmap? LoadFallbackBitmap()
    {
        try
        {
            using var stream = AssetLoader.Open(new Uri("avares://RedgifsDownloader.Avalonia/Assets/icon.ico"));
            return new Bitmap(stream);
        }
        catch
        {
            return null;
        }
    }
}
