using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text.Json;
using RedgifsDownloader.ApplicationLayer.Fikfap;
using RedgifsDownloader.Domain.Interfaces;

namespace RedgifsDownloader.Infrastructure.Fikfap
{
    internal class FikfapApiClient : IFikfapApiClient
    {
        private readonly HttpClient _http;
        private readonly string _token;

        public FikfapApiClient(HttpClient http, FikfapSession session)
        {
            _http = http;
            _token = session.Token;
        }

        public async IAsyncEnumerable<string> StreamUserPostsJson(string username, [EnumeratorCancellation] CancellationToken ct = default)
        {
            string? afterId = null;
            int amount = 21;
            while (true)
            {
                string url = afterId == null
                    ? $"https://api.fikfap.com/profile/username/{username}/posts?amount={amount}"
                    : $"https://api.fikfap.com/profile/username/{username}/posts?amount={amount}&afterId={afterId}";

                using var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Add("authorization-anonymous", _token);
                request.Headers.Add("accept", "*/*");
                request.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/121.0.0.0 Safari/537.36");
                request.Headers.Add("accept-language", "zh-CN,zh;q=0.9,en;q=0.8");
                request.Headers.Add("Origin", "https://fikfap.com");
                request.Headers.Add("Referer", $"https://fikfap.com/user/{username}");

                using var response = await _http.SendAsync(request, ct);
                if (!response.IsSuccessStatusCode)
                    yield break;

                string json = await response.Content.ReadAsStringAsync(ct);
                yield return json;

                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                if (root.ValueKind != JsonValueKind.Array)
                    yield break;

                var posts = root.EnumerateArray().ToList();

                if (posts.Count < amount)
                    yield break;

                afterId = posts.Last().GetProperty("postId").ToString();
            }
        }
    }
}
