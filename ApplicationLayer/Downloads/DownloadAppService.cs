using System.IO;
using System.Runtime.CompilerServices;
using RedgifsDownloader.ApplicationLayer.DTOs;
using RedgifsDownloader.ApplicationLayer.Fikfap;
using RedgifsDownloader.ApplicationLayer.Interfaces;
using RedgifsDownloader.Domain.Entities;
using RedgifsDownloader.Domain.Enums;
using RedgifsDownloader.Domain.Interfaces;

namespace RedgifsDownloader.ApplicationLayer.Downloads
{
    public class DownloadAppService : IDownloadAppService
    {
        private readonly IMediaCrawlerFactory _crawlerFactory;
        private readonly IVideoPathStrategy _pathStrategy;
        private readonly IPlatformDownloadStrategy _platformStrategy;
        private readonly IFileStorage _fileStorage;
        private readonly ILogService _logger;
        private readonly FikfapSession _session;

        public DownloadAppService(IMediaCrawlerFactory mediaCrawlerFactory,
            IVideoPathStrategy pathStrategy,
            IPlatformDownloadStrategy platformStrategy,
            IFileStorage fileStorage,
            FikfapSession session,
            ILogService logger)
        {
            _crawlerFactory = mediaCrawlerFactory;
            _pathStrategy = pathStrategy;
            _platformStrategy = platformStrategy;
            _fileStorage = fileStorage;
            _logger = logger;
            _session = session;
        }

        public async IAsyncEnumerable<Video> CrawlAsync(MediaPlatform platform, string username, Action<string>? onError = null, [EnumeratorCancellation] CancellationToken ct = default)
        {
            var crawler = _crawlerFactory.Create(platform);

            crawler.OnError += msg => onError?.Invoke(msg);

            await foreach (var dto in crawler.CrawlAsync(username, ct))
            {
                yield return ConvertToDomain(dto);
            }
        }

        public async Task<DownloadSummary> DownloadAsync(IEnumerable<Video> videos, int concurrency, CancellationToken ct = default)
        {
            var summary = new DownloadSummary();

            using var semaphore = new SemaphoreSlim(concurrency);

            var tasks = videos.Select(async video =>
            {
                try { await semaphore.WaitAsync(ct); }
                catch(OperationCanceledException) { return; }
                
                try
                {
                    string outputPath = _pathStrategy.BuildDownloadPath(video);
                    _fileStorage.CreateDirectory(Path.GetDirectoryName(outputPath));

                    if (File.Exists(outputPath))
                    {
                        video.MarkExists();
                        video.SetProgress(100);
                        summary.Completed++;
                        return;
                    }

                    video.MarkDownloading();

                    var downloader = _platformStrategy.Resolve(video.Platform);

                    var context = BuildDownloadContext(video);
                    var progress = new Progress<double>(p => { video.SetProgress(p); });

                    DownloadResult result;
                    try
                    {
                        result = await downloader.DownloadAsync(video.Url, outputPath, context, ct, progress);
                    }
                    catch (OperationCanceledException)
                    {
                        video.MarkFailed();
                        summary.Failed++;
                        return;
                    }

                    if (result.Status == VideoStatus.Completed)
                    {
                        video.MarkCompleted();
                        summary.Completed++;
                    }
                    else
                    {
                        video.MarkFailed();
                        summary.Failed++;
                    }
                }
                finally { semaphore.Release(); }
            });

            await Task.WhenAll(tasks);
            return summary;
        }

        public Task<DownloadSummary> RetryFailedAsync(IEnumerable<Video> failedVideos, int concurrency, CancellationToken ct = default)
        {
            return DownloadAsync(failedVideos, concurrency, ct);
        }

        private Video ConvertToDomain(VideoDto dto)
        {
            return new Video(
                id: dto.Id,
                username: dto.Username,
                url: new Uri(dto.Url),
                createDateRaw: dto.CreateDateRaw,
                token: dto.Token,
                platform: dto.Platform);
        }

        /// TODO: 在appservice里直接定义headers不美观，考虑搬出来
        private MediaDownloadContext BuildDownloadContext(Video video)
        {
            var headers = new Dictionary<string, string> { ["User-Agent"] = "Mozilla/5.0" };

            switch (video.Platform)
            {
                case MediaPlatform.Redgifs:
                    headers["Referer"] = "https://www.redgifs.com/";
                    headers["Origin"] = "https://www.redgifs.com/";
                    headers["Accept"] = "application/json, text/plain, */*";
                    if (!string.IsNullOrEmpty(video.Token))
                        headers["Authorization"] = $"Bearer {video.Token}";
                    break;

                case MediaPlatform.Fikfap:
                    headers["Referer"] = "https://www.fikfap.com";
                    headers["origin"] = "https://www.fikfap.com";
                    headers["accept"] = "*/*";
                    headers["accept-encoding"] = "gzip, deflate, br";
                    headers["authorization-anonymous"] = _session.Token;
                    break;
            }

            return new MediaDownloadContext
            {
                Headers = headers
            };
        }
    }
}
