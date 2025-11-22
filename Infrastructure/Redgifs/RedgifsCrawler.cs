using System.Runtime.CompilerServices;
using RedgifsDownloader.ApplicationLayer.DTOs;
using RedgifsDownloader.ApplicationLayer.Interfaces;
using RedgifsDownloader.Domain.Enums;

namespace RedgifsDownloader.Infrastructure.Redgifs
{
    public class RedgifsCrawler : IMediaCrawler
    {
        private readonly RedgifsApiClient _apiClient;

        public event Action<string>? OnError;

        public RedgifsCrawler(RedgifsApiClient apiClient)
        {
            _apiClient = apiClient;
        }

        public async IAsyncEnumerable<VideoDto> CrawlAsync(string username, [EnumeratorCancellation] CancellationToken ct = default)
        {
            await foreach (var video in _apiClient.FetchUserVideosAsync(username, pageSize: 40, ct))
            {
                if (video?.Id == null || video.Urls == null)
                    continue;

                var videoUrl = video.Urls.Hd ?? video.Urls.Sd;
                if (string.IsNullOrEmpty(videoUrl))
                    continue;

                yield return new VideoDto(
                    id: video.Id,
                    username: video.UserName ?? username,
                    url: videoUrl,
                    createDataRaw: video.CreateDate,
                    token: null, // Redgifs 不需要单独的视频 token
                    platform: MediaPlatform.Redgifs,
                    thumbnailUrl: video.Urls.Thumbnail 
                );
            }
        }
    }
}
