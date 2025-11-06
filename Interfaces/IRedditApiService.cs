using RedgifsDownloader.Model;
using RedgifsDownloader.Model.Reddit;
using System.Text.Json;

namespace RedgifsDownloader.Interfaces
{
    public interface IRedditApiService
    {
        IAsyncEnumerable<JsonElement> StreamUserPostsAsync(string username);
        IAsyncEnumerable<RedditPost> StreamUserImagePostsAsync(string username);
        IAsyncEnumerable<VideoItem> StreamUserRedgifsPostsAsync(string username);
    }
}
