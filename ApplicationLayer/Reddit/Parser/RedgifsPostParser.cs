using System.Text.Json;
using System.Text.RegularExpressions;
using RedgifsDownloader.ApplicationLayer.DTOs;

namespace RedgifsDownloader.ApplicationLayer.Reddit.Parser
{
    public class RedgifsPostParser
    {
        private static readonly Regex SlugRegex =
        new(@"media\.redgifs\.com/([A-Za-z0-9_-]+)-poster\.jpg", RegexOptions.IgnoreCase);

        public static IEnumerable<VideoDto> Extract(JsonElement post)
        {
            if (!IsRedgifsPost(post)) yield break;

            string? slug = ExtractSlug(post);
            if (slug == null) yield break;

            string author = post.GetProperty("author").GetString() ?? "unknown";
            string title = post.GetProperty("title").GetString() ?? "";

            yield return new VideoDto
            {
                Id = title,
                Username = author,
                Url = $"https://media.redgifs.com/{slug}.mp4"
            };
        }

        private static bool IsRedgifsPost(JsonElement data)
        {
            foreach (var key in new[] { "media", "secure_media" })
            {
                if (!data.TryGetProperty(key, out var media)) continue;

                if (media.TryGetProperty("type", out var typeNode) &&
                    typeNode.GetString()?.Contains("redgifs.com") == true)
                    return true;
            }

            return false;
        }

        private static string? ExtractSlug(JsonElement post)
        {
            foreach (var key in new[] { "media", "secure_media" })
            {
                if (!post.TryGetProperty(key, out var media)) continue;

                if (media.TryGetProperty("oembed", out var oembed) &&
                    oembed.TryGetProperty("thumbnail_url", out var thumb))
                {
                    string? t = thumb.GetString();
                    if (t == null) continue;
                    var m = SlugRegex.Match(t);
                    if (m.Success) return m.Groups[1].Value;
                }
            }
            return null;
        }
    }
}
