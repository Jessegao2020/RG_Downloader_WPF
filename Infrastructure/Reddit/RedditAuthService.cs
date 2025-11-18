using RedgifsDownloader.Domain.Interfaces;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Web;

namespace RedgifsDownloader.Infrastructure.Reddit
{
    public class RedditAuthService : IRedditAuthService
    {
        private readonly HttpClient _http;
        private readonly string _clientId = "iM34IicIqcttie1nwDJNhQ";
        private readonly string _redirectUri = "http://127.0.0.1:13579/";
        private readonly string[] _scopes = new[] { "identity", "read", "history" };
        private readonly string _userAgent = "Redgifsdownloader/1.0 (by u/test_user)";

        private string _accessToken = null!;
        private string _refreshToken = null!;
        private DateTime _expiresAt;

        private readonly string _tokenFile = System.IO.Path.Combine(AppContext.BaseDirectory, "reddit_token.json");

        public bool IsLoggedIn => !string.IsNullOrEmpty(_accessToken) && DateTime.UtcNow < _expiresAt;

        public RedditAuthService(HttpClient http)
        {
            _http = http;
            LoadTokens();
        }

        public async Task<string> GetAccessTokenAsync()
        {
            if (IsLoggedIn)
                return _accessToken;

            if (!string.IsNullOrEmpty(_refreshToken))
            {
                await RefreshTokenAsync();
                SaveTokens();
                return _accessToken;
            }

            await LoginAsync();
            SaveTokens();
            return _accessToken;
        }

        private async Task RefreshTokenAsync()
        {
            var authBytes = Encoding.ASCII.GetBytes($"{_clientId}:");
            _http.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", Convert.ToBase64String(authBytes));
            _http.DefaultRequestHeaders.Add("User-Agent", _userAgent);

            var post = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string,string>("grant_type","refresh_token"),
                new KeyValuePair<string,string>("refresh_token",_refreshToken)
            });

            var resp = await _http.PostAsync("https://www.reddit.com/api/v1/access_token", post);
            string body = await resp.Content.ReadAsStringAsync();
            if (!resp.IsSuccessStatusCode)
                throw new Exception($"Refresh failed: {resp.StatusCode}: {body}");

            using var doc = JsonDocument.Parse(body);
            _accessToken = doc.RootElement.GetProperty("access_token").GetString()!;
            int expiresIn = doc.RootElement.GetProperty("expires_in").GetInt32();
            _expiresAt = DateTime.UtcNow.AddSeconds(expiresIn - 60);
        }

        public async Task LoginAsync()
        {
            string state = Guid.NewGuid().ToString("N");
            string scopeStr = string.Join(" ", _scopes);
            string authorizeUrl =
                $"https://www.reddit.com/api/v1/authorize?client_id={_clientId}" +
                $"&response_type=code&state={state}" +
                $"&redirect_uri={Uri.EscapeDataString(_redirectUri)}" +
                $"&duration=permanent&scope={Uri.EscapeDataString(scopeStr)}";

            // 启动轻量级 TCP 回调监听（代替 HttpListener）
            var waitTask = WaitForRedirectAsync(13579);

            // 打开 Reddit 授权页面
            Process.Start(new ProcessStartInfo
            {
                FileName = authorizeUrl,
                UseShellExecute = true
            });

            var (code, returnedState) = await waitTask;

            if (returnedState != state)
                throw new Exception("OAuth state mismatch.");

            await ExchangeCodeForTokenAsync(code);
            SaveTokens();
        }

        private void LoadTokens()
        {
            try
            {
                if (!File.Exists(_tokenFile))
                    return;

                byte[] encrypted = File.ReadAllBytes(_tokenFile);
                byte[] decrypted = ProtectedData.Unprotect(encrypted, null, DataProtectionScope.CurrentUser);

                using var doc = JsonDocument.Parse(decrypted);
                _refreshToken = doc.RootElement.GetProperty("refresh").GetString()!;
                _expiresAt = doc.RootElement.GetProperty("expires").GetDateTime();
            }
            catch { }
        }

        private void SaveTokens()
        {
            try
            {
                var json = JsonSerializer.Serialize(new
                {
                    refresh = _refreshToken,
                    expires = _expiresAt
                });

                byte[] data = Encoding.UTF8.GetBytes(json);
                byte[] encrypted = ProtectedData.Protect(data, null, DataProtectionScope.CurrentUser);

                File.WriteAllBytes(_tokenFile, encrypted);
            }
            catch { }
        }

        private async Task<(string Code, string State)> WaitForRedirectAsync(int port)
        {
            var listener = new TcpListener(IPAddress.Loopback, port);
            listener.Start();

            using var client = await listener.AcceptTcpClientAsync();
            using var stream = client.GetStream();
            using var reader = new StreamReader(stream, Encoding.ASCII, false, 1024, leaveOpen: true);
            using var writer = new StreamWriter(stream, new UTF8Encoding(false)) { NewLine = "\r\n", AutoFlush = true };

            string? line = await reader.ReadLineAsync();
            if (line == null || !line.StartsWith("GET "))
                throw new Exception("Invalid HTTP request.");

            // 解析 ?code= 和 ?state=
            string path = line.Split(' ')[1];
            var uri = new Uri("http://127.0.0.1" + path);
            var query = HttpUtility.ParseQueryString(uri.Query);

            string code = query["code"] ?? throw new Exception("No code returned.");
            string state = query["state"] ?? "";

            // 丢弃剩余 header
            while (!string.IsNullOrEmpty(await reader.ReadLineAsync())) { }

            string html = "<html><body><h2>Authorization successful.</h2>You may close this window.</body></html>";
            byte[] body = Encoding.UTF8.GetBytes(html);
            await writer.WriteLineAsync("HTTP/1.1 200 OK");
            await writer.WriteLineAsync("Content-Type: text/html; charset=utf-8");
            await writer.WriteLineAsync($"Content-Length: {body.Length}");
            await writer.WriteLineAsync();
            await stream.WriteAsync(body);

            listener.Stop();
            return (code, state);
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
    }
}
