using System.Text.Json;
using RedgifsDownloader.ApplicationLayer.Settings;

namespace RedgifsDownloader.Avalonia;

internal sealed class AvaloniaAppSettings : IAppSettings
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    private static readonly string SettingsFilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "RedgifsDownloader",
        "settings.json");

    public int MaxConcurrentDownloads { get; set; }

    public string DownloadDirectory { get; set; }

    public AvaloniaAppSettings()
    {
        var defaults = CreateDefaults();
        DownloadDirectory = defaults.DownloadDirectory;
        MaxConcurrentDownloads = defaults.MaxConcurrentDownloads;

        LoadFromFile();
    }

    public void Save()
    {
        var directory = Path.GetDirectoryName(SettingsFilePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        DownloadDirectory = NormalizeDirectory(DownloadDirectory, CreateDefaults().DownloadDirectory);
        MaxConcurrentDownloads = ClampConcurrent(MaxConcurrentDownloads);

        var dto = new SettingsDto
        {
            DownloadDirectory = DownloadDirectory,
            MaxConcurrentDownloads = MaxConcurrentDownloads
        };

        var json = JsonSerializer.Serialize(dto, JsonOptions);
        File.WriteAllText(SettingsFilePath, json);
    }

    private void LoadFromFile()
    {
        try
        {
            if (!File.Exists(SettingsFilePath))
            {
                return;
            }

            var json = File.ReadAllText(SettingsFilePath);
            var dto = JsonSerializer.Deserialize<SettingsDto>(json, JsonOptions);
            var defaults = CreateDefaults();

            DownloadDirectory = NormalizeDirectory(dto?.DownloadDirectory, defaults.DownloadDirectory);
            MaxConcurrentDownloads = ClampConcurrent(dto?.MaxConcurrentDownloads ?? defaults.MaxConcurrentDownloads);
        }
        catch
        {
            var defaults = CreateDefaults();
            DownloadDirectory = defaults.DownloadDirectory;
            MaxConcurrentDownloads = defaults.MaxConcurrentDownloads;
        }
    }

    private static SettingsDto CreateDefaults() => new()
    {
        DownloadDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
            "RG_Downloads"),
        MaxConcurrentDownloads = 3
    };

    private static int ClampConcurrent(int value) => Math.Clamp(value, 1, 20);

    private static string NormalizeDirectory(string? path, string fallback)
        => string.IsNullOrWhiteSpace(path) ? fallback : path.Trim();

    private sealed class SettingsDto
    {
        public string DownloadDirectory { get; set; } = string.Empty;
        public int MaxConcurrentDownloads { get; set; } = 3;
    }
}
