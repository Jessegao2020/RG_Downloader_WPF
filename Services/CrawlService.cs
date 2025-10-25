using System.Diagnostics;
using System.Text.Json;
using RedgifsDownloader.Model;

namespace RedgifsDownloader.Services
{
    public class CrawlService
    {
        public async Task<List<VideoItem>> CrawlAsync (string userName, Action<string>? onError = null)
        {
            string spiderPath = System.IO.Path.Combine(AppContext.BaseDirectory, "videoSpider");

            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "python",
                    WorkingDirectory = spiderPath,
                    Arguments = $"-u -m scrapy crawl videos -a user={userName} --loglevel ERROR",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            var results = new List<VideoItem>();

            process.OutputDataReceived += (_, e) =>
            {
                if (string.IsNullOrEmpty(e.Data)) return;

                try
                {
                    var video = JsonSerializer.Deserialize<VideoItem>(e.Data);
                    if (video != null)
                    {
                        video.Status = VideoStatus.Pending;
                        lock(results) results.Add(video);
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"JSON parse error: {ex.Message}: {e.Data}");
                }
            };

            process.ErrorDataReceived += (_, e) =>
            {
                if (!string.IsNullOrWhiteSpace(e.Data) && e.Data.StartsWith("ERROR_MSG:"))
                    onError?.Invoke(e.Data.Substring("ERROR_MSG:".Length).Trim());
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            await process.WaitForExitAsync();
            return results;
        }
    }
}
