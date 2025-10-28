using RedgifsDownloader.Model.Reddit;

namespace RedgifsDownloader.Interfaces
{
    public interface IRedditApiService
    {
        Task<string> GetUserSubmittedAsync(string username);
        Task<List<RedditPost>> GetUserImagePostsAsync(string username);
    }
}
