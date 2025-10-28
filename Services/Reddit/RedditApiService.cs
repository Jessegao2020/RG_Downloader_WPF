using RedgifsDownloader.Interfaces;
using System.Net.Http;

namespace RedgifsDownloader.Services.Reddit
{
    public class RedditApiService : IRedditApiService
    {
        private readonly IRedditAuthService _auth;
        private readonly HttpClient _http;

        public RedditApiService(IRedditAuthService auth)
        {
            _auth = auth;
            _http = new HttpClient();
        }

        public async Task<string> GetUserSubmittedAsync(string username){
            string token = await _auth.GetAccessTokenAsync();
            _http.DefaultRequestHeaders.Clear();
            _http.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
            _http.DefaultRequestHeaders.Add("User-Agent", "Redgifsdownloader/1.0 (by u/test_user)");

            var response = await _http.GetAsync($"https://oauth.reddit.com/user/{username}/submitted");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }
    }
}
