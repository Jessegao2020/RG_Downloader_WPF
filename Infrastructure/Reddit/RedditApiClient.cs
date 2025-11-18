using RedgifsDownloader.Domain.Interfaces;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;

namespace RedgifsDownloader.Infrastructure.Reddit
{
    public class RedditApiClient : IRedditApiClient
    {
        private readonly HttpClient _http;
        private readonly IRedditAuthService _auth;

        public RedditApiClient(HttpClient http, IRedditAuthService auth)
        {
            _http = http;
            _auth = auth;
        }

        public async IAsyncEnumerable<string> StreamUserPostsJson(string username, CancellationToken ct = default)
        {
            string token = await _auth.GetAccessTokenAsync();

            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("bearer", token);
            _http.DefaultRequestHeaders.UserAgent.ParseAdd("Redgifsdownloader/1.0 (by u/test_user)");
            _http.DefaultRequestHeaders.Add("Cookie", "over18=1;");

            string? after = null;

            do
            {
                string url = $"https://oauth.reddit.com/user/{username}/submitted?limit=100&raw_json=1&include_over_18=on";

                if (after != null)
                    url += $"&after={after}";

                using var response = await _http.GetAsync(url, ct);
                response.EnsureSuccessStatusCode();

                string json = await response.Content.ReadAsStringAsync();

                yield return json;

                var doc = JsonDocument.Parse(json);
                var data = doc.RootElement.GetProperty("data");

                after = data.TryGetProperty("after", out var afterElem)
                    && afterElem.ValueKind == JsonValueKind.String
                    ? afterElem.GetString()
                    : null;

                await Task.Delay(1000, ct);
            } while (after != null);
        }

    }
}
