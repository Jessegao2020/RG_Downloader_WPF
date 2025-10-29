using RedgifsDownloader.Interfaces;
using System.Diagnostics;
using System.IO;
using System.Net.Http;

namespace RedgifsDownloader.Services.Reddit
{
    public class RedditImageDownloadService
    {
        private readonly HttpClient _http;

        private readonly ILogService _logger;

        public RedditImageDownloadService(HttpClient client, ILogService logger)
        {
            _http = client;
            _logger = logger;
        }

        public async Task<bool> DownloadAsync(string url, string path)
        {
            try
            {
                var bytes = await _http.GetByteArrayAsync(url);
                await File.WriteAllBytesAsync(path, bytes);
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Download Error] {url}: {ex.Message}");
                return false;
            }
        }
    }
}
