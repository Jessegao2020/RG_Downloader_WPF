namespace RedgifsDownloader.ApplicationLayer.Reddit
{
    public interface IRedditDownloadAppService
    {
        Task<bool> LoginAsync();

        Task<RedditDownloadSummary> DownloadUserAsync(
            string username,
            bool isVideoMode,
            int concurrency,
            Action<string>? log = null,
            Action<int>? progress = null,
            CancellationToken ct = default);
    }

    public record RedditDownloadSummary(int Success, int Fail);
}
