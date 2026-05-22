using RedgifsDownloader.ApplicationLayer.Settings;

namespace RedgifsDownloader.Avalonia;

internal sealed class AvaloniaAppSettings : IAppSettings
{
    public int MaxConcurrentDownloads { get; set; } = 1;

    public string DownloadDirectory { get; set; } =
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
            "RG_Downloads");

    public void Save()
    {
        // Minimal Avalonia version: in-memory settings only for now.
    }
}
