using RedgifsDownloader.ApplicationLayer.DTOs;
using RedgifsDownloader.Domain.Enums;

namespace RedgifsDownloader.Domain.Interfaces
{
    public interface ITransferDownloader
    {
        Task<DownloadResult> DownloadAsync(
            Uri url,
            string outputPath,
            MediaDownloadContext context,
            CancellationToken ct = default,
            IProgress<double>? progress = null);
    }
}
