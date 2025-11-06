using System.Net.Http;
using System.Text.Json;

namespace RedgifsDownloader.Services
{
    public class RedgifsAuthService
    {
        private readonly HttpClient _http = new();
        private string? _token;
        private DateTime _expiry;

        public async Task<string> GetTokenAsync()
        {
            // 如果 token 还没过期，直接返回
            if (!string.IsNullOrEmpty(_token) && DateTime.UtcNow < _expiry)
                return _token;

            var req = new HttpRequestMessage(HttpMethod.Get, "https://api.redgifs.com/v2/auth/temporary");
            req.Headers.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64)");
            req.Headers.Referrer = new Uri("https://www.redgifs.com/");
            req.Headers.Add("Origin", "https://www.redgifs.com");
            req.Headers.Add("Accept", "application/json, text/plain, */*");

            var resp = await _http.SendAsync(req);
            resp.EnsureSuccessStatusCode();

            string json = await resp.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            _token = doc.RootElement.GetProperty("token").GetString();
            _expiry = DateTime.UtcNow.AddMinutes(90); // 假设 1.5 小时过期

            return _token!;
        }
    }
}
