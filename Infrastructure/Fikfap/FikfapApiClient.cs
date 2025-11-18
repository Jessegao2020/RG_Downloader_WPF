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
                request.Headers.Add("User-Agent", "Mozilla/5.0");
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
