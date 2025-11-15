using RedgifsDownloader.Domain.Enums;

namespace RedgifsDownloader.ApplicationLayer.Interfaces
{
    public interface IMediaCrawlerFactory
    {
        IMediaCrawler Create(MediaPlatform platfrom);
    }
}
