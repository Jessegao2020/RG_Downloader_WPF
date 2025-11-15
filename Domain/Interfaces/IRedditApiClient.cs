namespace RedgifsDownloader.Domain.Interfaces
{
    public interface IRedditApiClient
    {
        IAsyncEnumerable<string> StreamUserPostsJson(string username, CancellationToken ct = default);
    }
}
