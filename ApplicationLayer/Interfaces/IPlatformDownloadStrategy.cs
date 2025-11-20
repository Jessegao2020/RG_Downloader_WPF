using RedgifsDownloader.Domain.Enums;
using RedgifsDownloader.Domain.Interfaces;

namespace RedgifsDownloader.ApplicationLayer.Interfaces
{
    public interface IPlatformDownloadStrategy
    {
        ITransferDownloader SelectDownloader(MediaPlatform platform);
    }
}
