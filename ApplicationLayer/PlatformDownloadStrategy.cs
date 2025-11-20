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
        private readonly YtDlpTransferDownloader _ytdlp;

        public PlatformDownloadStrategy(HttpTransferDownloader http, YtDlpTransferDownloader ytdlp)
        {
            _http = http;
            _ytdlp = ytdlp;
        }

        public ITransferDownloader SelectDownloader(MediaPlatform platform)
        {
            return platform switch
            {
                MediaPlatform.Redgifs => _http,
                MediaPlatform.Fikfap => _ytdlp,
                _ => throw new NotSupportedException($"{platform} not supported.")
            };
        }
    }
}
