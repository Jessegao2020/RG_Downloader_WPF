
using System.IO;
using RedgifsDownloader.ApplicationLayer.Settings;
using RedgifsDownloader.Domain.Enums;
using RedgifsDownloader.Domain.Interfaces;
using RedgifsDownloader.Interfaces;

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

            if (!isVideoMode)
            {
                await foreach (var img in _imageApp.Execute(username).WithCancellation(ct))
                {
                    count++;
                    progress?.Invoke(count);

                    string filename = $"{img.Id}_{Path.GetFileName(img.Url)}";
                    string output = Path.Combine(downloadDir, filename);

                    try
                    {
                        var result = await _downloader.DownloadAsync(new Uri(img.Url), output, new Domain.Enums.MediaDownloadContext(), ct);

                        if (result.Status == VideoStatus.Completed || result.Status == VideoStatus.Exists)
                            success++;
                        else
                            fail++;

                    }
                    catch (Exception ex)
                    {
                        fail++;
                        log($"[IMG]{filename}下载失败:{ex.Message}");
                    }
                }
            }
            else
            {
                string token = await _auth.GetAccessTokenAsync();
                log($"[Reddit] Redgifs Token 获取完成");

                await foreach (var video in _redgifsApp.Execute(username).WithCancellation(ct))
                {
                    count++;
                    progress?.Invoke(count);

                    string filename = $"{video.Id}.mp4";
                    string output = Path.Combine(downloadDir, filename);

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
                            success++;
                        else
                            fail++;
                    }
                    catch (Exception ex)
                    {
                        fail++;
                        log($"[Video] {filename} 下载失败: {ex.Message}");
                    }
                }
            }

            return new RedditDownloadSummary(success, fail);
        }
    }
}
