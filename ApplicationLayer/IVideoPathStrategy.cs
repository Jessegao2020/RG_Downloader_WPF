using RedgifsDownloader.Domain.Entities;

namespace RedgifsDownloader.ApplicationLayer
{
    public interface IVideoPathStrategy
    {
        string BuildDownloadPath(Video video);
    }
}
