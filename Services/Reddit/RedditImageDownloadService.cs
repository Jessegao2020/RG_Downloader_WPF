using System.Diagnostics;
using System.IO;
using System.Net.Http;

namespace RedgifsDownloader.Services.Reddit
{
    public class RedditImageDownloadService
    {
        private readonly HttpClient _http = new();

        public async Task DownloadImageAsync(IEnumerable<string> urls, string baseDir)
        {
            Directory.CreateDirectory(baseDir);

            foreach (string url in urls)
            {
                try
                {
                    string fileName = Path.GetFileName(new Uri(url).LocalPath);
                    string path = Path.Combine(baseDir, fileName);

                    if (File.Exists(path))
                        continue;

                    var bytes = await _http.GetByteArrayAsync(url);
                    await File.WriteAllBytesAsync(path, bytes);
                }
                catch (Exception ex) { Debug.WriteLine($"[DownloadError] {url} => {ex.Message}"); } 
            }
        }
    }
}
