using RedgifsDownloader.ApplicationLayer.DTOs;
using RedgifsDownloader.ApplicationLayer.Interfaces;
using RedgifsDownloader.ApplicationLayer.Settings;
using RedgifsDownloader.ApplicationLayer.Utils;
using RedgifsDownloader.Domain.Enums;
using RedgifsDownloader.Domain.Interfaces;
using System.IO;

namespace RedgifsDownloader.ApplicationLayer.Reddit
{
    public class RedditDownloadAppService : IRedditDownloadAppService
    {
        private readonly IRedditAuthService _auth;
        private readonly ITransferDownloader _downloader;
        private readonly IAppSettings _settings;
        private readonly IUserNotificationService _logger;
        private readonly IFileStorage _fileStorage;
        private readonly RedditFetchImagesAppService _imageApp;
        private readonly RedditFetchRedgifsAppService _redgifsApp;

        public RedditDownloadAppService(
            IRedditAuthService auth,
            ITransferDownloader downloader,
            IAppSettings settings,
            IUserNotificationService logger,
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
                await _auth.LoginAsync().ConfigureAwait(false);
                return true;
            }
            catch (Exception ex)
            {
                // 这里如果 _logger 是 UI 弹窗服务，建议它内部自己 dispatch 到 UI 线程
                _logger.ShowMessage($"[Reddit] 登陆失败: {ex.Message}");
                return false;
            }
        }

        public async Task<RedditDownloadSummary> DownloadUserAsync(
            string username,
            bool isVideoMode,
            int concurrency,
            IProgress<string>? log = null,
            IProgress<int>? progress = null,
            CancellationToken ct = default)
        {
            string downloadDir = Path.Combine(_settings.DownloadDirectory, username);
            Directory.CreateDirectory(downloadDir);

            var stats = new DownloadStats();

            using var semaphore = new SemaphoreSlim(concurrency);
            var tasks = new List<Task>();

            if (!isVideoMode)
            {
                await foreach (var img in _imageApp.Execute(username)
                                                   .WithCancellation(ct)
                                                   .ConfigureAwait(false))
                {
                    string filename = FileNameSanitizer.MakeSafeFileName(img.Title, img.Id, img.Url);
                    string output = Path.Combine(downloadDir, filename);

                    if (_fileStorage.FileExistsWithCommonExtensions(output))
                    {
                        Interlocked.Increment(ref stats.Skipped);
                        log?.Report($"[Skip] {filename}");
                        continue;
                    }

                    await semaphore.WaitAsync(ct).ConfigureAwait(false);

                    tasks.Add(DownloadImageAsync(img, output, semaphore, ct, log, progress, stats));
                }
            }
            else
            {
                string token = await _auth.GetAccessTokenAsync().ConfigureAwait(false);
                log?.Report("[Reddit] Redgifs Token 获取完成");

                var seenVideos = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                await foreach (var video in _redgifsApp.Execute(username)
                                                      .WithCancellation(ct)
                                                      .ConfigureAwait(false))
                {
                    string filename = Path.GetFileName(new Uri(video.Url).AbsolutePath);
                    if (!seenVideos.Add(filename))
                        continue;

                    string output = Path.Combine(downloadDir, filename);

                    if (_fileStorage.FileExistsWithCommonExtensions(output))
                    {
                        Interlocked.Increment(ref stats.Skipped);
                        log?.Report($"[Skip] {filename}");
                        continue;
                    }

                    await semaphore.WaitAsync(ct).ConfigureAwait(false);

                    tasks.Add(DownloadVideoAsync(video, output, token, semaphore, ct, log, progress, stats));
                }
            }

            await Task.WhenAll(tasks).ConfigureAwait(false);
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
            IProgress<string>? log,
            IProgress<int>? progress,
            DownloadStats stats)
        {
            try
            {
                string filename = Path.GetFileName(output);

                var result = await _downloader.DownloadAsync(
                    new Uri(img.Url),
                    output,
                    new MediaDownloadContext(),
                    ct).ConfigureAwait(false);

                if (result.Status == VideoStatus.Completed || result.Status == VideoStatus.Exists)
                {
                    int newCount = Interlocked.Increment(ref stats.Downloaded);
                    progress?.Report(newCount);
                    log?.Report($"[Finished] {filename}");
                }
                else
                {
                    Interlocked.Increment(ref stats.Failed);
                    log?.Report($"[Failed] {filename}");
                }
            }
            catch (Exception ex)
            {
                Interlocked.Increment(ref stats.Failed);
                log?.Report($"[IMG]下载失败: {ex.Message}");
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
            IProgress<string>? log,
            IProgress<int>? progress,
            DownloadStats stats)
        {
            try
            {
                string filename = Path.GetFileName(output);

                var context = new MediaDownloadContext
                {
                    Headers = new()
                    {
                        { "Authorization", "Bearer " + token },
                        { "User-Agent", "Mozilla/5.0" }
                    }
                };

                var result = await _downloader.DownloadAsync(
                    new Uri(video.Url),
                    output,
                    context,
                    ct).ConfigureAwait(false);

                if (result.Status == VideoStatus.Completed)
                {
                    int newCount = Interlocked.Increment(ref stats.Downloaded);
                    progress?.Report(newCount);
                    log?.Report($"[Finish] {filename}");
                }
                else
                {
                    Interlocked.Increment(ref stats.Failed);
                    log?.Report($"[Failed] {filename}");
                }
            }
            catch (Exception ex)
            {
                Interlocked.Increment(ref stats.Failed);
                log?.Report($"[Video] 下载失败: {ex.Message}");
            }
            finally
            {
                try { semaphore.Release(); } catch (ObjectDisposedException) { }
            }
        }
    }
}