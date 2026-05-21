using RedgifsDownloader.ApplicationLayer.Settings;

namespace RedgifsDownloader.Avalonia.Services;

public sealed class AvaloniaAppSettings : IAppSettings
{
    public int MaxConcurrentDownloads { get; set; } = 1;
    public string DownloadDirectory { get; set; } = Path.Combine(AppContext.BaseDirectory, "Downloads");
    public void Save() { }
}
