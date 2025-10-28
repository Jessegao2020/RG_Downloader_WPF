using RedgifsDownloader.Model.Reddit;

namespace RedgifsDownloader.Interfaces
{
    public interface IRedditApiService
    {
        IAsyncEnumerable<RedditPost> StreamUserImagePostsAsync(string username);
    }
}
