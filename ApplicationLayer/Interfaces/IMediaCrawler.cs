using RedgifsDownloader.ApplicationLayer.DTOs;

namespace RedgifsDownloader.ApplicationLayer.Interfaces
{
    public interface IMediaCrawler
    {
        event Action<string>? OnError;

        IAsyncEnumerable<VideoDto> CrawlAsync(string username, CancellationToken ct = default);
    }
}
