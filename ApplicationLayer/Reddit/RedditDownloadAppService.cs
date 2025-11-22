using System.IO;
using RedgifsDownloader.ApplicationLayer.DTOs;
using RedgifsDownloader.ApplicationLayer.Settings;
using RedgifsDownloader.ApplicationLayer.Utils;
using RedgifsDownloader.Domain.Enums;
using RedgifsDownloader.Domain.Interfaces;

namespace RedgifsDownloader.ApplicationLayer.Reddit
{
    public class RedditDownloadAppService : IRedditDownloadAppService
    {
        private readonly IRedditAuthService _auth;
        private readonly ITransferDownloader _downloader;
        private readonly IAppSettings _settings;
        private readonly ILogService _logger;
        private readonly IFileStorage _fileStorage;
        private readonly RedditFetchImagesAppService _imageApp;
        private readonly RedditFetchRedgifsAppService _redgifsApp;

        public RedditDownloadAppService(
            IRedditAuthService auth,
            ITransferDownloader downloader,
            IAppSettings settings,
            ILogService logger,
            IFileStorage fileStorage,
            RedditFetchImagesAppService imageApp,
            RedditFetchRedgifsAppService redgifsApp)
        {
            _auth = auth;
            _downloader = downloader;
            _settings = settings;
            _logger = logger;
            _fileStorage = fileStorage;
            _imageApp = imageApp;
            _redgifsApp = redgifsApp;
        }

        public async Task<bool> LoginAsync()
        {
            try
            {
                await _auth.LoginAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.ShowMessage($"[Reddit] 登陆失败: {ex.Message}");
                return false;
            }
        }

        public async Task<RedditDownloadSummary> DownloadUserAsync(
            string username,
            bool isVideoMode,
            int concurrency,
            Action<string>? log = null,
            Action<int>? progress = null,
            CancellationToken ct = default)
        {
            log ??= _ => { };

            string downloadDir = Path.Combine(_settings.DownloadDirectory, username);
            Directory.CreateDirectory(downloadDir);

            int downloaded = 0;
            int failed = 0;
            int skipped = 0;

            // 使用 stats 对象来在异步方法间共享计数
            var stats = new DownloadStats();

            using var semaphore = new SemaphoreSlim(concurrency);
            var tasks = new List<Task>();

            if (!isVideoMode)
            {
                await foreach (var img in _imageApp.Execute(username).WithCancellation(ct))
                {
                    string filename = FileNameSanitizer.MakeSafeFileName(img.Title, img.Id, img.Url);
                    string output = Path.Combine(downloadDir, filename);

                    if (_fileStorage.FileExistsWithCommonExtensions(output))
                    {
                        Interlocked.Increment(ref stats.Skipped);
                        log?.Invoke($"[Skip] {filename}");
                        continue;
                    }

                    await semaphore.WaitAsync(ct);

                    tasks.Add(DownloadImageAsync(img, output, semaphore, ct, log, progress, stats));
                }
            }
            else
            {
                string token = await _auth.GetAccessTokenAsync();
                log($"[Reddit] Redgifs Token 获取完成");

                var seenVideos = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                await foreach (var video in _redgifsApp.Execute(username).WithCancellation(ct))
                {
                    string filename = Path.GetFileName(new Uri(video.Url).AbsolutePath);

                    if (!seenVideos.Add(filename))
                        continue;

                    string output = Path.Combine(downloadDir, filename);

                    if (_fileStorage.FileExistsWithCommonExtensions(output))
                    {
                        Interlocked.Increment(ref stats.Skipped);
                        log?.Invoke($"[Skip] {filename}");
                        continue;
                    }

                    await semaphore.WaitAsync(ct);

                    tasks.Add(DownloadVideoAsync(video, output, token, semaphore, ct, log, progress, stats));
                }
            }

            await Task.WhenAll(tasks);
            return new RedditDownloadSummary(stats.Downloaded, stats.Failed, stats.Skipped);
        }

        private class DownloadStats
        {
            public int Downloaded;
            public int Failed;
            public int Skipped;
        }

        private async Task DownloadImageAsync(
            RedditPostDto img,
            string output,
            SemaphoreSlim semaphore,
            CancellationToken ct,
            Action<string>? log,
            Action<int>? progress,
            DownloadStats stats)
        {
            try
            {
                string filename = Path.GetFileName(output); // 从 output 重新获取文件名用于日志
                var result = await _downloader.DownloadAsync(new Uri(img.Url), output, new Domain.Enums.MediaDownloadContext(), ct);
                if (result.Status == VideoStatus.Completed || result.Status == VideoStatus.Exists)
                {
                    int newCount = Interlocked.Increment(ref stats.Downloaded);
                    progress?.Invoke(newCount);
                    log?.Invoke($"[Finished] {filename}");
                }
                else
                {
                    Interlocked.Increment(ref stats.Failed);
                    log?.Invoke($"[Failed] {filename}");
                }
            }
            catch (Exception ex)
            {
                Interlocked.Increment(ref stats.Failed);
                log?.Invoke($"[IMG]下载失败:{ex.Message}");
            }
            finally
            {
                try { semaphore.Release(); } catch (ObjectDisposedException) { }
            }
        }

        private async Task DownloadVideoAsync(
            VideoDto video,
            string output,
            string token,
            SemaphoreSlim semaphore,
            CancellationToken ct,
            Action<string>? log,
            Action<int>? progress,
            DownloadStats stats)
        {
            try
            {
                string filename = Path.GetFileName(output);
                var context = new MediaDownloadContext();
                context.Headers = new()
                {
                    { "Authorization", "Bearer " + token },
                    { "User-Agent", "Mozilla/5.0" }
                };
                var result = await _downloader.DownloadAsync(new Uri(video.Url), output, context, ct);

                if (result.Status == VideoStatus.Completed)
                {
                    int newCount = Interlocked.Increment(ref stats.Downloaded);
                    log?.Invoke($"[Finish] {filename}");
                    progress?.Invoke(newCount);
                }
                else
                    Interlocked.Increment(ref stats.Failed);
            }
            catch (Exception ex)
            {
                Interlocked.Increment(ref stats.Failed);
                log?.Invoke($"[Video] 下载失败: {ex.Message}");
            }
            finally
            {
                try { semaphore.Release(); } catch (ObjectDisposedException) { }
            }
        }
    }
}
