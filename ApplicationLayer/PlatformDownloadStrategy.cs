using RedgifsDownloader.ApplicationLayer.Interfaces;
using RedgifsDownloader.Domain.Enums;
using RedgifsDownloader.Domain.Interfaces;
using RedgifsDownloader.Infrastructure;
using RedgifsDownloader.Infrastructure.Fikfap;

namespace RedgifsDownloader.ApplicationLayer
{
    public class PlatformDownloadStrategy : IPlatformDownloadStrategy
    {
        private readonly HttpTransferDownloader _http;
        private readonly FikfapM3u8Downloader _fikfapM3u8;

        public PlatformDownloadStrategy(HttpTransferDownloader http, FikfapM3u8Downloader fikfapM3u8)
        {
            _http = http;
            _fikfapM3u8 = fikfapM3u8;
        }

        public ITransferDownloader SelectDownloader(MediaPlatform platform)
        {
            return platform switch
            {
                MediaPlatform.Redgifs => _http,
                MediaPlatform.Fikfap => _fikfapM3u8,
                _ => throw new NotSupportedException($"{platform} not supported.")
            };
        }
    }
}
