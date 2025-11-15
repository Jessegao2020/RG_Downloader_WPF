using System.Text.Json;
using RedgifsDownloader.ApplicationLayer.DTOs;
using RedgifsDownloader.ApplicationLayer.Reddit.Parser;
using RedgifsDownloader.Domain.Interfaces;

namespace RedgifsDownloader.ApplicationLayer.Reddit
{
    public class RedditFetchRedgifsAppService
    {
        private readonly IRedditApiClient _api;

        public RedditFetchRedgifsAppService(IRedditApiClient api)
        {
            _api = api;
        }

        public async IAsyncEnumerable<VideoDto> Execute(string username)
        {
            await foreach (var json in _api.StreamUserPostsJson(username))
            {
                var doc = JsonDocument.Parse(json);

                foreach (var post in RedditPostParser.EnumerateChildren(doc))
                {
                    foreach (var video in RedgifsPostParser.Extract(post))
                        yield return video;
                }
            }
        }
    }
}
