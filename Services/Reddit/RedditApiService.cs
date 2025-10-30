using System.Diagnostics;
using System.Net.Http;
using System.Text.Json;
using RedgifsDownloader.Helpers;
using RedgifsDownloader.Interfaces;
using RedgifsDownloader.Model.Reddit;

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

        public async IAsyncEnumerable<RedditPost> StreamUserImagePostsAsync(string username)
        {
            string token = await _auth.GetAccessTokenAsync();
            _http.DefaultRequestHeaders.Clear();
            _http.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("bearer", token);
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

                string? json = null;
                try
                {
                    var response = await _http.GetAsync(url);

                    if (!response.IsSuccessStatusCode)
                    {
                        var errorbody = await response.Content.ReadAsStringAsync();
                        _logger.ShowMessage($"[Reddit API]: {response.StatusCode}");
                        yield break;
                    }

                    json = await response.Content.ReadAsStringAsync();
                }
                catch (Exception ex)
                {
                    _logger.ShowMessage($"网络异常: {ex.Message}");
                }

                using var doc = JsonDocument.Parse(json);
                var data = doc.RootElement.GetProperty("data");

                var pagePosts = RedditPostParser.ExtractImagePosts(data);

                foreach (var post in pagePosts)
                    yield return post;  //理解返回发现的图片

                // 取分页游标

                if (data.TryGetProperty("after", out var afterElem) && afterElem.ValueKind == JsonValueKind.String)
                    after = afterElem.GetString();

                else
                    after = null;

                Debug.WriteLine($"[RedditApi] 第 {page} 页，累计 {pagePosts.Count} 张图片。");

                await Task.Delay(1000); // 避免429限流
            }
            while (after != null);
        }
    }
}
