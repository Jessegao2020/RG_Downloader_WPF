using System.Net.Http;
using RedgifsDownloader.ApplicationLayer.DTOs;
using RedgifsDownloader.Domain.Enums;
using RedgifsDownloader.Domain.Interfaces;
using RedgifsDownloader.Infrastructure.M3u8;

namespace RedgifsDownloader.Infrastructure.Fikfap
{
    public class FikfapM3u8Downloader : ITransferDownloader
    {
        private readonly GenericM3u8Downloader _downloader;

        public FikfapM3u8Downloader(HttpClient httpClient)
        {
            var streamSelector = new AvcPreferredStreamSelector();
            _downloader = new GenericM3u8Downloader(httpClient, streamSelector);
        }

        public async Task<DownloadResult> DownloadAsync(
            Uri url,
            string outputPath,
            MediaDownloadContext context,
            CancellationToken ct = default,
            IProgress<double>? progress = null)
        {
            return await _downloader.DownloadAsync(url, outputPath, context, ct, progress);
        }
    }
}
