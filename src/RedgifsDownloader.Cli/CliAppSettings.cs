using RedgifsDownloader.ApplicationLayer.Settings;

namespace RedgifsDownloader.Cli;

public class CliAppSettings : IAppSettings
{
    public int MaxConcurrentDownloads { get; set; } = 4;

    public string DownloadDirectory { get; set; } = Path.Combine(AppContext.BaseDirectory, "Downloads");

    public void Save()
    {
        // No-op for CLI prototype.
    }
}
