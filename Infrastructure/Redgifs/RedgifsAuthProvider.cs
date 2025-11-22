using System.Diagnostics;
using System.Net.Http;
using System.Text.Json;
using RedgifsDownloader.Domain.Enums;
using RedgifsDownloader.Domain.Interfaces;

namespace RedgifsDownloader.Infrastructure.Redgifs
{
    public class RedgifsAuthProvider : IPlatformAuthProvider
    {
        private readonly HttpClient _http;
        private string? _token;
        private DateTime _expiry;

        public RedgifsAuthProvider(HttpClient http)
        {
            _http = http;
        }

        public async Task<PlatformAuthResult> GetTokenAsync(CancellationToken ct = default)
        {
            if (!string.IsNullOrEmpty(_token) && DateTime.UtcNow < _expiry)
                return new PlatformAuthResult(_token, _expiry);

            var request = new HttpRequestMessage(HttpMethod.Get, "https://api.redgifs.com/v2/auth/temporary");

            AddPlatformHeaders(request);

            var response = await _http.SendAsync(request, ct);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(ct);
                Debug.WriteLine($"[RedgifsAuth] Token 获取失败: {errorContent}");
                throw new HttpRequestException($"Failed to get Redgifs token: {response.StatusCode}");
            }

            var jsonContent = await response.Content.ReadAsStringAsync(ct);
            
            using var doc = JsonDocument.Parse(jsonContent);
            _token = doc.RootElement.GetProperty("token").GetString();
            _expiry = DateTime.UtcNow.AddMinutes(90);

            return new PlatformAuthResult(_token, _expiry);
        }

        private void AddPlatformHeaders(HttpRequestMessage request)
        {
            request.Headers.TryAddWithoutValidation("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/141.0.0.0 Safari/537.36");
            request.Headers.TryAddWithoutValidation("Referer", "https://www.redgifs.com/");
            request.Headers.TryAddWithoutValidation("Accept", "application/json, text/plain, */*");
        }
    }
}
