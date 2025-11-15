using RedgifsDownloader.Domain.Enums;

namespace RedgifsDownloader.Domain.Interfaces
{
    public record DownloadResult(VideoStatus Status, long TotalBytes);
    
}
