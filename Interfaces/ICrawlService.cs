using RedgifsDownloader.Model;

namespace RedgifsDownloader.Interfaces
{
    public interface ICrawlService
    {
        IAsyncEnumerable<VideoItem> CrawlAsync(string userName, Action<string>? onError = null);
    }
}
