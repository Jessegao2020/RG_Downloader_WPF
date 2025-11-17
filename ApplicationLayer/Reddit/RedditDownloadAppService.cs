using RedgifsDownloader.ApplicationLayer.Settings;
using RedgifsDownloader.Domain.Enums;
using RedgifsDownloader.Domain.Interfaces;
using RedgifsDownloader.Interfaces;
using System.IO;

namespace RedgifsDownloader.ApplicationLayer.Reddit
{
    public class RedditDownloadAppService : IRedditDownloadAppService
    {
        private readonly RedgifsDownloader.Domain.Interfaces.IRedditAuthService _auth;
        private readonly RedditFetchImagesAppService _imageApp;
        private readonly RedditFetchRedgifsAppService _redgifsApp;
        private readonly IMediaDownloader _downloader;
        private readonly IAppSettings _settings;
        private readonly ILogService _logger;

        public RedditDownloadAppService(
            Domain.Interfaces.IRedditAuthService auth,
            RedditFetchImagesAppService imageApp,
            RedditFetchRedgifsAppService redgifsApp,
            IMediaDownloader downloader,
            IAppSettings settings, ILogService logger)
        {
            _auth = auth;
            _imageApp = imageApp;
            _redgifsApp = redgifsApp;
            _downloader = downloader;
            _settings = settings;
            _logger = logger;
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

            int success = 0;
            int fail = 0;
            int count = 0;

            using var semaphore = new SemaphoreSlim(concurrency);
            var tasks = new List<Task>();

            if (!isVideoMode)
            {
                await foreach (var img in _imageApp.Execute(username).WithCancellation(ct))
                {
                    await semaphore.WaitAsync(ct);

                    tasks.Add(Task.Run(async () =>
                    {
                        try
                        {
                            Interlocked.Increment(ref count);
                            progress?.Invoke(count);
                            string filename = $"{img.Title}_{Path.GetFileName(img.Url)}";
                            string output = Path.Combine(downloadDir, filename);

                            var result = await _downloader.DownloadAsync(new Uri(img.Url), output, new Domain.Enums.MediaDownloadContext(), ct);
                            if (result.Status == VideoStatus.Completed || result.Status == VideoStatus.Exists)
                                Interlocked.Increment(ref success);
                            else
                                Interlocked.Increment(ref fail);
                        }
                        catch (Exception ex)
                        {
                            Interlocked.Increment(ref fail);
                            log($"[IMG]下载失败:{ex.Message}");
                        }
                        finally { semaphore.Release(); }
                    }, ct));
                }
            }
            else
            {
                string token = await _auth.GetAccessTokenAsync();
                log($"[Reddit] Redgifs Token 获取完成");

                await foreach (var video in _redgifsApp.Execute(username).WithCancellation(ct))
                {
                    await semaphore.WaitAsync(ct);

                    tasks.Add(Task.Run(async () =>
                    {
                        try
                        {
                            Interlocked.Increment(ref count);
                            progress?.Invoke(count);

                            string filename = $"{video.Id}.mp4";
                            string output = Path.Combine(downloadDir, filename);

                            var context = new MediaDownloadContext();
                            context.Headers = new()
                            {
                                { "Authorization", "Bearer " + token },
                                { "User-Agent", "Mozilla/5.0" }
                            };
                            var result = await _downloader.DownloadAsync(new Uri(video.Url), output, context, ct);

                            if (result.Status == VideoStatus.Completed)
                                Interlocked.Increment(ref success);
                            else
                                Interlocked.Increment(ref fail);
                        }
                        catch (Exception ex)
                        {
                            Interlocked.Increment(ref fail);
                            log($"[Video] 下载失败: {ex.Message}");

                        }
                        finally { semaphore.Release(); }
                    }, ct));
                }
            }

            await Task.WhenAll(tasks);
            return new RedditDownloadSummary(success, fail);
        }
    }
}
