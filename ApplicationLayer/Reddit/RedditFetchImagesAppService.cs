using RedgifsDownloader.ApplicationLayer.DTOs;
using RedgifsDownloader.ApplicationLayer.Reddit.Parser;
using RedgifsDownloader.Domain.Interfaces;
using System.Text.Json;

namespace RedgifsDownloader.ApplicationLayer.Reddit
{
    public class RedditFetchImagesAppService
    {
        private readonly IRedditApiClient _api;

        public RedditFetchImagesAppService(IRedditApiClient api)
        {
            _api = api;
        }

        public async IAsyncEnumerable<RedditPostDto> Execute(string username)
        {
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            await foreach (var json in _api.StreamUserPostsJson(username))
            {
                using var doc = JsonDocument.Parse(json);

                foreach (var post in RedditPostParser.EnumerateChildren(doc))
                {
                    foreach (var img in ImagePostParser.Extract(post))
                    {
                        if (!seen.Add(img.Url))
                            continue;

                        yield return img;
                    }
                }
            }
        }
    }
}
