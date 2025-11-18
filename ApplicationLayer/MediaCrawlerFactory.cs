using Microsoft.Extensions.DependencyInjection;
using RedgifsDownloader.ApplicationLayer.Interfaces;
using RedgifsDownloader.Domain.Enums;
using RedgifsDownloader.Infrastructure.Redgifs;

namespace RedgifsDownloader.ApplicationLayer
{
    internal class MediaCrawlerFactory : IMediaCrawlerFactory
    {
        private readonly IServiceProvider _sp;

        public MediaCrawlerFactory(IServiceProvider sp) { _sp = sp; }

        public IMediaCrawler Create(MediaPlatform platfrom)
        {
            return platfrom switch
            {
                MediaPlatform.Redgifs => _sp.GetRequiredService<RedgifsCrawler>(),
                _ => throw new NotSupportedException($"{platfrom} is not supported")
            };
        }
    }
}
