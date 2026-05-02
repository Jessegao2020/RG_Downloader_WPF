namespace RedgifsDownloader.ApplicationLayer.Reddit
{
    public interface IRedditDownloadAppService
    {
        Task<bool> LoginAsync();

        Task<RedditDownloadSummary> DownloadUserAsync(
            string username,
            bool isVideoMode,
            int concurrency,
            DateTimeOffset? minCreatedUtc = null,
            IProgress<string>? log = null,
            IProgress<int>? progress = null,
            CancellationToken ct = default);
    }

    public record RedditDownloadSummary(int Success, int Fail, int Skip);
}
