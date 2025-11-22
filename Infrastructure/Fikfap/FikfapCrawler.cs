using System.Runtime.CompilerServices;
using RedgifsDownloader.ApplicationLayer.DTOs;
using RedgifsDownloader.ApplicationLayer.Interfaces;
using RedgifsDownloader.ApplicationLayer.Fikfap;
using RedgifsDownloader.Domain.Interfaces;

namespace RedgifsDownloader.Infrastructure.Fikfap
{
    public class FikfapCrawler : IMediaCrawler
    {
        public event Action<string>? OnError;

        private readonly IFikfapApiClient _api;

        public FikfapCrawler(IFikfapApiClient api) { _api = api; }

        public async IAsyncEnumerable<VideoDto> CrawlAsync(string username, [EnumeratorCancellation] CancellationToken ct = default)
        {
            await foreach (var json in _api.StreamUserPostsJson(username, ct).WithCancellation(ct))
            {
                foreach (var dto in FikfapParser.Extract(json))
                {
                    yield return dto;
                }
            }
        }
    }
}
