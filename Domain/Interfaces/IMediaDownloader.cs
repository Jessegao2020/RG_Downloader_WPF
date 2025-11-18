using RedgifsDownloader.Domain.Enums;

namespace RedgifsDownloader.Domain.Interfaces
{
    public interface IMediaDownloader
    {
        Task<DownloadResult> DownloadAsync(
            Uri url,
            string outputPath,
            MediaDownloadContext context,
            CancellationToken ct = default,
            IProgress<double>? progress = null);
    }
}
