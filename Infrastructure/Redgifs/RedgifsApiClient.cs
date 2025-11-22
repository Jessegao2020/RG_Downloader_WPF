using System.Diagnostics;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text.Json;
using RedgifsDownloader.Domain.Interfaces;

namespace RedgifsDownloader.Infrastructure.Redgifs
{
    public class RedgifsApiClient
    {
        private readonly HttpClient _http;
        private readonly IPlatformAuthProvider _authProvider;

        public RedgifsApiClient(HttpClient http, IPlatformAuthProvider authProvider)
        {
            _http = http;
            _authProvider = authProvider;
        }

        public async IAsyncEnumerable<RedgifsVideoResponse> FetchUserVideosAsync(
            string username, 
            int pageSize = 40,
            [EnumeratorCancellation] CancellationToken ct = default)
        {
            int page = 1;
            bool hasMore = true;

            while (hasMore && !ct.IsCancellationRequested)
            {
                var pageData = await FetchPageAsync(username, page, pageSize, ct);
                
                if (pageData?.Gifs == null || pageData.Gifs.Count == 0)
                {
                    hasMore = false;
                    yield break;
                }

                foreach (var gif in pageData.Gifs)
                {
                    yield return gif;
                }

                page++;
            }
        }

        private async Task<RedgifsPageResponse?> FetchPageAsync(
            string username, 
            int page, 
            int count,
            CancellationToken ct)
        {
            // 获取最新 Token
            var authResult = await _authProvider.GetTokenAsync(ct);
            var url = $"https://api.redgifs.com/v2/users/{username}/search?order=new&count={count}&page={page}";

            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.TryAddWithoutValidation("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/141.0.0.0 Safari/537.36");
            request.Headers.TryAddWithoutValidation("Referer", "https://www.redgifs.com/");
            request.Headers.TryAddWithoutValidation("Origin", "https://www.redgifs.com");
            request.Headers.TryAddWithoutValidation("Authorization", $"Bearer {authResult.Token}");
            request.Headers.TryAddWithoutValidation("Accept", "application/json, text/plain, */*");

            try
            {
                var response = await _http.SendAsync(request, ct);
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(ct);
                    return null;
                }

                var json = await response.Content.ReadAsStringAsync(ct);
                
                return JsonSerializer.Deserialize<RedgifsPageResponse>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Redgifs API Error] Page {page}: {ex.Message}");
                return null;
            }
        }
    }

    #region Response Models

    public class RedgifsPageResponse
    {
        public List<RedgifsVideoResponse>? Gifs { get; set; }
        public int Page { get; set; }
        public int Pages { get; set; }
        public int Total { get; set; }
    }

    public class RedgifsVideoResponse
    {
        public string? Id { get; set; }
        public string? UserName { get; set; }
        public RedgifsUrls? Urls { get; set; }
        public long? CreateDate { get; set; }
    }

    public class RedgifsUrls
    {
        public string? Hd { get; set; }
        public string? Sd { get; set; }
        public string? Thumbnail { get; set; }  // Thumbnail 在 urls 对象里
        public string? Poster { get; set; }
    }
    #endregion
}

