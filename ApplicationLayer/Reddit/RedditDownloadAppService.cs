using RedgifsDownloader.ApplicationLayer.Settings;
using RedgifsDownloader.ApplicationLayer.Utils;
using RedgifsDownloader.Domain.Enums;
using RedgifsDownloader.Domain.Interfaces;
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

            int downloaded = 0;
            int failed = 0;
            int skipped = 0;

            using var semaphore = new SemaphoreSlim(concurrency);
            var tasks = new List<Task>();

            if (!isVideoMode)
            {
                await foreach (var img in _imageApp.Execute(username).WithCancellation(ct))
                {
                    string filename = FIleNameSanitizer.MakeSafeFileName(img.Title, img.Id, img.Url);
                    string output = Path.Combine(downloadDir, filename);

                    if (File.Exists(output))
                    {
                        Interlocked.Increment(ref skipped);
                        log?.Invoke($"[Skip] {filename}");
                        continue;
                    }

                    await semaphore.WaitAsync(ct);

                    tasks.Add(Task.Run(async () =>
                    {
                        try
                        {
                            var result = await _downloader.DownloadAsync(new Uri(img.Url), output, new Domain.Enums.MediaDownloadContext(), ct);
                            if (result.Status == VideoStatus.Completed || result.Status == VideoStatus.Exists)
                            {
                                int newCount = Interlocked.Increment(ref downloaded);
                                progress?.Invoke(newCount);
                                log?.Invoke($"[Finished] {filename}");
                            }
                            else
                            {
                                Interlocked.Increment(ref failed);
                                log?.Invoke($"[Failed] {filename}");
                            }
                        }
                        catch (Exception ex)
                        {
                            Interlocked.Increment(ref failed);
                            log?.Invoke($"[IMG]下载失败:{ex.Message}");
                        }
                        finally { semaphore.Release(); }
                    }, ct));
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

                    if (File.Exists(output))
                    {
                        Interlocked.Increment(ref skipped);
                        log?.Invoke($"[Skip] {filename}");
                        continue;
                    }

                    await semaphore.WaitAsync(ct);

                    tasks.Add(Task.Run(async () =>
                    {
                        try
                        {
                            var context = new MediaDownloadContext();
                            context.Headers = new()
                            {
                                { "Authorization", "Bearer " + token },
                                { "User-Agent", "Mozilla/5.0" }
                            };
                            var result = await _downloader.DownloadAsync(new Uri(video.Url), output, context, ct);

                            if (result.Status == VideoStatus.Completed)
                            {
                                int newCount = Interlocked.Increment(ref downloaded);
                                log?.Invoke($"[Finish] {filename}");
                                progress?.Invoke(newCount);
                            }
                            else
                                Interlocked.Increment(ref failed);
                        }
                        catch (Exception ex)
                        {
                            Interlocked.Increment(ref failed);
                            log?.Invoke($"[Video] 下载失败: {ex.Message}");
                        }
                        finally { semaphore.Release(); }
                    }, ct));
                }
            }

            await Task.WhenAll(tasks);
            return new RedditDownloadSummary(downloaded, failed, skipped);
        }
    }
}
