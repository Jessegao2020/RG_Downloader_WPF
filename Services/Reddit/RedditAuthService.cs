using RedgifsDownloader.Interfaces;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace RedgifsDownloader.Services.Reddit
{
    public class RedditAuthService : IRedditAuthService
    {
        private readonly string _clientId = "iM34IicIqcttie1nwDJNhQ";
        private readonly string _redirectUri = "http://localhost:7890/redirect/";
        private readonly string[] _scopes = new[] { "identity", "read" };
        private readonly string _userAgent = "Redgifsdownloader/1.0 (by u/test_user)";

        private string _accessToken = null!;
        private string _refreshToken = null!;
        private DateTime _expiresAt;

        public bool IsLoggedIn => !string.IsNullOrEmpty(_accessToken) && DateTime.UtcNow < _expiresAt;

        public async Task LoginAsync()
        {
            string state = Guid.NewGuid().ToString("N");
            string scopeStr = string.Join(" ", _scopes);
            string authorizeUrl = $"https://www.reddit.com/api/v1/authorize?client_id={_clientId}" +
            $"&response_type=code&state={state}&redirect_uri={Uri.EscapeDataString(_redirectUri)}" +
            $"&duration=permanent&scope={Uri.EscapeDataString(scopeStr)}";


            var listenerTask = Task.Run(async () =>
            {
                using var listener = new HttpListener();
                listener.Prefixes.Add(_redirectUri);
                listener.Start();
                var context = await listener.GetContextAsync();

                var resp = context.Response;
                string html = "<html><body><h2>Authorization successful.</h2>You may close this window.</body></html>";
                byte[] buffer = Encoding.UTF8.GetBytes(html);
                resp.ContentLength64 = buffer.Length;
                await resp.OutputStream.WriteAsync(buffer);
                resp.OutputStream.Close();
                return context.Request.QueryString;
            });

            // 浏览器授权
            Process.Start(new ProcessStartInfo { FileName = authorizeUrl, UseShellExecute = true });

            var query = await listenerTask;
            if (query["error"] != null) throw new Exception($"OAuth error: {query["error"]}");
            string code = query["code"] ?? throw new Exception("No code in callback");
            await ExchangeCodeForTokenAsync(code);
        }

        private async Task ExchangeCodeForTokenAsync(string code)
        {
            using var http = new HttpClient();
            var authBytes = Encoding.ASCII.GetBytes($"{_clientId}:");
            http.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", Convert.ToBase64String(authBytes));
            http.DefaultRequestHeaders.Add("User-Agent", _userAgent);

            var post = new FormUrlEncodedContent(new[]
            {
            new KeyValuePair<string,string>("grant_type","authorization_code"),
            new KeyValuePair<string,string>("code",code),
            new KeyValuePair<string,string>("redirect_uri",_redirectUri)
        });

            var resp = await http.PostAsync("https://www.reddit.com/api/v1/access_token", post);
            string body = await resp.Content.ReadAsStringAsync();
            if (!resp.IsSuccessStatusCode)
                throw new Exception($"Token exchange failed: {resp.StatusCode}: {body}");

            using var doc = JsonDocument.Parse(body);
            _accessToken = doc.RootElement.GetProperty("access_token").GetString()!;
            _refreshToken = doc.RootElement.TryGetProperty("refresh_token", out var rt) ? rt.GetString()! : null!;
            int expiresIn = doc.RootElement.GetProperty("expires_in").GetInt32();
            _expiresAt = DateTime.UtcNow.AddSeconds(expiresIn - 60);
        }

        public async Task<string> GetAccessTokenAsync()
        {
            if (IsLoggedIn)
                return _accessToken;

            if (!string.IsNullOrEmpty(_refreshToken))
                await RefreshTokenAsync();
            else
                throw new Exception("Not logged in");

            return _accessToken;
        }

        private async Task RefreshTokenAsync()
        {
            using var http = new HttpClient();
            var authBytes = Encoding.ASCII.GetBytes($"{_clientId}:");
            http.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", Convert.ToBase64String(authBytes));
            http.DefaultRequestHeaders.Add("User-Agent", _userAgent);

            var post = new FormUrlEncodedContent(new[]
            {
            new KeyValuePair<string,string>("grant_type","refresh_token"),
            new KeyValuePair<string,string>("refresh_token",_refreshToken)
        });

            var resp = await http.PostAsync("https://www.reddit.com/api/v1/access_token", post);
            string body = await resp.Content.ReadAsStringAsync();
            if (!resp.IsSuccessStatusCode)
                throw new Exception($"Refresh failed: {resp.StatusCode}: {body}");

            using var doc = JsonDocument.Parse(body);
            _accessToken = doc.RootElement.GetProperty("access_token").GetString()!;
            int expiresIn = doc.RootElement.GetProperty("expires_in").GetInt32();
            _expiresAt = DateTime.UtcNow.AddSeconds(expiresIn - 60);
        }
    }
}
