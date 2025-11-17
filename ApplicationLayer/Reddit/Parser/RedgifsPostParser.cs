using System.Text.Json;
using System.Text.RegularExpressions;
using RedgifsDownloader.ApplicationLayer.DTOs;

namespace RedgifsDownloader.ApplicationLayer.Reddit.Parser
{
    public class RedgifsPostParser
    {
        private static readonly Regex PosterRegex =
        new(@"media\.redgifs\.com/([A-Za-z0-9_-]+)-poster\.jpg", RegexOptions.IgnoreCase);

        public static IEnumerable<VideoDto> Extract(JsonElement post)
        {
            foreach (var key in new[] { "media", "secure_media" })
            {
                if (!post.TryGetProperty(key, out var media))
                    continue;

                if (media.ValueKind != JsonValueKind.Object)
                    continue; // 🔥 media 可能是 null，跳过避免崩溃

                // ---- 判断是不是 redgifs ----
                if (!media.TryGetProperty("type", out var typeNode))
                    continue;

                if (typeNode.ValueKind != JsonValueKind.String)
                    continue;

                var typeStr = typeNode.GetString();
                if (typeStr == null || !typeStr.Contains("redgifs.com", StringComparison.OrdinalIgnoreCase))
                    continue;

                // ---- 解析 oembed.thumbnail_url ----
                if (!media.TryGetProperty("oembed", out var oembed) || oembed.ValueKind != JsonValueKind.Object)
                    continue;

                if (!oembed.TryGetProperty("thumbnail_url", out var thumbNode))
                    continue;

                if (thumbNode.ValueKind != JsonValueKind.String)
                    continue;

                string? thumbUrl = thumbNode.GetString();
                if (thumbUrl == null)
                    continue;

                // ---- 提取 slug ----
                var m = PosterRegex.Match(thumbUrl);
                if (!m.Success)
                    continue;

                string slug = m.Groups[1].Value;

                // ---- 构造 VideoDto ----
                yield return new VideoDto
                {
                    Id = SafeGetString(post, "title") ?? "",
                    Username = SafeGetString(post, "author") ?? "unknown",
                    Url = $"https://media.redgifs.com/{slug}.mp4"
                };
            }
        }

        private static string? SafeGetString(JsonElement post, string prop)
        {
            if (post.TryGetProperty(prop, out var elem) && elem.ValueKind == JsonValueKind.String)
                return elem.GetString();

            return null;
        }
    }
}
