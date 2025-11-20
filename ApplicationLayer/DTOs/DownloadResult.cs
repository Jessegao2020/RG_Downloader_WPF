using RedgifsDownloader.Domain.Enums;

namespace RedgifsDownloader.ApplicationLayer.DTOs
{
    public record DownloadResult(VideoStatus Status, long TotalBytes);

}
