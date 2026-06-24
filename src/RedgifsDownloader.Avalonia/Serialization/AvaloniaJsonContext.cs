using System.Text.Json.Serialization;
using Avalonia.Controls;

namespace RedgifsDownloader.Avalonia.Serialization;

[JsonSourceGenerationOptions(
    PropertyNameCaseInsensitive = true,
    WriteIndented = true)]
[JsonSerializable(typeof(AvaloniaSettingsData))]
[JsonSerializable(typeof(WindowPlacementData))]
internal partial class AvaloniaJsonContext : JsonSerializerContext
{
}

internal sealed class AvaloniaSettingsData
{
    public string DownloadDirectory { get; set; } = string.Empty;

    public int MaxConcurrentDownloads { get; set; } = 3;
}

internal sealed record WindowPlacementData
{
    public int X { get; init; }

    public int Y { get; init; }

    public double Width { get; init; } = 450;

    public double Height { get; init; } = 550;

    public WindowState State { get; init; } = WindowState.Normal;
}
