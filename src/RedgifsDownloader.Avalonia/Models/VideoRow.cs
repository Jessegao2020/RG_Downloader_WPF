namespace RedgifsDownloader.Avalonia.Models;

public sealed class VideoRow
{
    public string Id { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string CreateDateRaw { get; set; } = string.Empty;
    public bool HasThumbnailUrl { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Progress { get; set; } = "-";
}
