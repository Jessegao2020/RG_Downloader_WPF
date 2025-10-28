using System.Net.Http;
using RedgifsDownloader.Helpers;
using RedgifsDownloader.Interfaces;
using RedgifsDownloader.Model.Reddit;

namespace RedgifsDownloader.Services.Reddit
{
    public class RedditApiService : IRedditApiService
    {
        private readonly IRedditAuthService _auth;
        private readonly HttpClient _http = new();

        public RedditApiService(IRedditAuthService auth)
        {
            _auth = auth;
        }

        public async Task<string> GetUserSubmittedAsync(string username)
        {
            string token = await _auth.GetAccessTokenAsync();

            _http.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("bearer", token);
            _http.DefaultRequestHeaders.UserAgent.ParseAdd("Redgifsdownloader/1.0 (by u/test_user)");

            try
            {
                var response = await _http.GetAsync($"https://oauth.reddit.com/user/{username}/submitted");
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadAsStringAsync(); ;
            }
            catch (Exception ex)
            {
                return $"Http error: {ex.Message}";
            }
        }

        public async Task<List<RedditPost>> GetUserImagePostsAsync(string username)
        {
            var json = await GetUserSubmittedAsync(username);
            return RedditPostParser.ExtractImagePosts(json);
        }
    }
}
