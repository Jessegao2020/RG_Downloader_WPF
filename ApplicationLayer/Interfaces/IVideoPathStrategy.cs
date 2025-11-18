using RedgifsDownloader.Domain.Entities;

namespace RedgifsDownloader.ApplicationLayer.Interfaces
{
    public interface IVideoPathStrategy
    {
        string BuildDownloadPath(Video video);
    }
}
