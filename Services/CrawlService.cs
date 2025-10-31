using System.Diagnostics;
using System.Text.Json;
using RedgifsDownloader.Interfaces;
using RedgifsDownloader.Model;

namespace RedgifsDownloader.Services
{
    public class CrawlService : ICrawlService
    {
        public async IAsyncEnumerable<VideoItem> CrawlAsync(string userName, Action<string>? onError = null)
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

            process.Start();

            _ = Task.Run(async () =>
            {
                while (!process.StandardError.EndOfStream)
                {
                    var err = await process.StandardError.ReadLineAsync();
                    if (!string.IsNullOrWhiteSpace(err) && err.StartsWith("ERROR_MSG:"))
                        onError?.Invoke(err.Substring("ERROR_MSG:".Length).Trim());
                }
            });

            while (!process.StandardOutput.EndOfStream)
            {
                var line = await process.StandardOutput.ReadLineAsync();
                if (string.IsNullOrWhiteSpace(line)) continue;

                VideoItem? parsed = null;
                try
                {
                    parsed = JsonSerializer.Deserialize<VideoItem>(line);
                    if (parsed != null) parsed.Status = VideoStatus.Pending;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"JSON parse error: {ex.Message}: {line}");
                }

                if (parsed != null)   // <- yield 在 try/catch 外
                    yield return parsed;
            }

            await process.WaitForExitAsync();
        }
    }
}
