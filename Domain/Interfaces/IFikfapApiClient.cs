namespace RedgifsDownloader.Domain.Interfaces
{
    public interface IFikfapApiClient
    {
        IAsyncEnumerable<string> StreamUserPostsJson(string username, CancellationToken ct = default);
    }
}
