using RedgifsDownloader.Helpers;
using RedgifsDownloader.Interfaces;
using RedgifsDownloader.Model;
using RedgifsDownloader.Model.Reddit;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;

namespace RedgifsDownloader.Services.Reddit
{
    public class RedditApiService : IRedditApiService
    {
        private readonly IRedditAuthService _auth;
        private readonly HttpClient _http = new();
        private readonly ILogService _logger;

        public RedditApiService(IRedditAuthService auth, ILogService logger)
        {
            _auth = auth;
            _logger = logger;
        }

        public async IAsyncEnumerable<JsonElement> StreamUserPostsAsync(string username)
        {
            string token = await _auth.GetAccessTokenAsync();
            _http.DefaultRequestHeaders.Clear();
            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("bearer", token);
            _http.DefaultRequestHeaders.UserAgent.ParseAdd("Redgifsdownloader/1.0 (by u/test_user)");
            _http.DefaultRequestHeaders.Add("Cookie", "over18=1;");

            string? after = null;
            int page = 0;

            do
            {
                page++;
                string url = $"https://oauth.reddit.com/user/{username}/submitted?limit=100&raw_json=1&include_over_18=on";
                if (!string.IsNullOrEmpty(after))
                    url += $"&after={after}";

                using var response = await _http.GetAsync(url);
                if (!response.IsSuccessStatusCode)
                    yield break;

                string json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                var data = doc.RootElement.GetProperty("data");

                yield return data;

                if (data.TryGetProperty("after", out var afterElem) && afterElem.ValueKind == JsonValueKind.String)
                    after = afterElem.GetString();
                else
                    after = null;

                await Task.Delay(1000);
            }
            while (after != null);
        }

        public async IAsyncEnumerable<RedditPost> StreamUserImagePostsAsync(string username)
        {
            await foreach (var data in StreamUserPostsAsync(username))
            {
                foreach (var post in RedditPostParser.ExtractImagePosts(data))
                    yield return post;
            }
        }

        public async IAsyncEnumerable<VideoItem> StreamUserRedgifsPostsAsync(string username)
        {
            await foreach (var data in StreamUserPostsAsync(username))
            {
                foreach (var video in RedditPostParser.ExtractRedgifsPosts(data))
                    yield return video;
            }
        }
    }
}
