using RedgifsDownloader.Model;

namespace RedgifsDownloader.Interfaces
{
    public interface ICrawlService
    {
        Task<List<VideoItem>> CrawlAsync(string userName, Action<string>? onError = null);
    }
}
