using RedgifsDownloader.Domain.Entities;
using RedgifsDownloader.Domain.Enums;

namespace RedgifsDownloader.ApplicationLayer.Downloads
{
    public interface IDownloadAppService
    {
        IAsyncEnumerable<Video> CrawlAsync(
            MediaPlatform platform,
            string usernmae,
            Action<string>? onError = null,
            CancellationToken ct = default);

        Task<DownloadSummary> DownloadAsync(IEnumerable<Video> videos, int concurrency, CancellationToken ct = default);

        Task<DownloadSummary> RetryFailedAsync(IEnumerable<Video> failedVideos, int concurrency, CancellationToken cancellationToken = default);
    }
}
