using System.Diagnostics;
using System.IO;
using System.Net.Http;

namespace RedgifsDownloader.Services.Reddit
{
    public class RedditImageDownloadService
    {
        private readonly HttpClient _http;

        public RedditImageDownloadService(HttpClient client)
        {
            _http = client;
        }

        public async Task<bool> DownloadAsync(string url, string path)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);

                using var response = await _http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
                response.EnsureSuccessStatusCode();

                await using var stream = await response.Content.ReadAsStreamAsync();
                await using var file = File.Create(path);
                await stream.CopyToAsync(file);

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
