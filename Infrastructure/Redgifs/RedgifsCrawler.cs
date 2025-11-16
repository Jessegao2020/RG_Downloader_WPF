using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text.Json;
using RedgifsDownloader.ApplicationLayer.DTOs;
using RedgifsDownloader.ApplicationLayer.Interfaces;
using RedgifsDownloader.Domain.Entities;
using RedgifsDownloader.Domain.Enums;
using RedgifsDownloader.Domain.Interfaces;
using RedgifsDownloader.Infrastructure.Redgifs.Models;

namespace RedgifsDownloader.Infrastructure.Redgifs
{
    public class RedgifsCrawler : IMediaCrawler
    {
        public event Action<string>? OnError;

        public async IAsyncEnumerable<VideoDto> CrawlAsync(string username, [EnumeratorCancellation] CancellationToken ct = default)
        {
            string spiderPath = System.IO.Path.Combine(AppContext.BaseDirectory, "videoSpider");

            using var process = CreateProcess(username, spiderPath);
            process.Start();

            var errorMonitor = MonitorErrors(process);



            while (!process.StandardOutput.EndOfStream)
            {
                ct.ThrowIfCancellationRequested();

                var line = await process.StandardOutput.ReadLineAsync();
                if (string.IsNullOrWhiteSpace(line)) continue;

                var raw = Deserialize(line);

                if (raw != null)
                    yield return ConverToDomain(raw);
            }
            await errorMonitor;
            await process.WaitForExitAsync();
        }

        private Process CreateProcess(string username, string path)
        {
            return new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "python",
                    WorkingDirectory = path,
                    Arguments = $"-u -m scrapy crawl videos -a user={username} --loglevel ERROR",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
        }

        private async Task MonitorErrors(Process process)
        {
            while (!process.StandardError.EndOfStream)
            {
                var err = await process.StandardError.ReadLineAsync();

                if (!string.IsNullOrWhiteSpace(err) && err.StartsWith("ERROR_MSG:"))
                {
                    var msg = err.Substring("ERROR_MSG:".Length).Trim();
                    OnError?.Invoke(msg); // 上报应用层
                }
            }
        }

        private RedgifsRawVideoDto? Deserialize(string json)
        {
            try
            {
                return JsonSerializer.Deserialize<RedgifsRawVideoDto>(json);
            }
            catch
            {
                return null;
            }
        }

        private VideoDto ConverToDomain(RedgifsRawVideoDto raw)
        {
            return new VideoDto(
                id: raw.Id!,
                username: raw.Username!,
                url: raw.Url!,
                createDataRaw: raw.CreateDateRaw,
                platform: MediaPlatform.Redgifs);
        }
    }
}
