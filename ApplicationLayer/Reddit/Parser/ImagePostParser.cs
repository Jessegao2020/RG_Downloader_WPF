using RedgifsDownloader.ApplicationLayer.DTOs;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace RedgifsDownloader.ApplicationLayer.Reddit.Parser
{
    public static class ImagePostParser
    {
        private static readonly Regex IdExtRegex =
            new(@"redd\.it/([A-Za-z0-9]+)\.(jpg|jpeg|png|webp)", RegexOptions.IgnoreCase);

        private static readonly Regex IdOnlyRegex =
            new(@"redd\.it/([A-Za-z0-9]+)", RegexOptions.IgnoreCase);

        private static readonly Regex FormatParamRegex =
            new(@"format=(png|jpg|jpeg|webp)", RegexOptions.IgnoreCase);

        public static IEnumerable<RedditPostDto> Extract(JsonElement post)
        {
            string title = post.GetProperty("title").GetString() ?? "";
            string author = post.GetProperty("author").GetString() ?? "unknown";

            // ---------------------
            // 1) 尝试主链接 url_overridden_by_dest
            // ---------------------
            string? url = TryGetMainUrl(post);
            url = ConvertPreviewToDirect(url);

            if (IsImageUrl(url))
            {
                yield return new RedditPostDto
                {
                    Id = ExtractIdFromUrl(url!),
                    Title = title,
                    Url = url!,
                    IsImage = true
                };
                yield break;  // ← 旧代码就是遇到直链优先返回
            }

            // ---------------------
            // 2) 解析相册 media_metadata
            // ---------------------
            if (post.TryGetProperty("media_metadata", out var meta) &&
                meta.ValueKind == JsonValueKind.Object)
            {
                foreach (var kv in meta.EnumerateObject())
                {
                    if (!kv.Value.TryGetProperty("s", out var sObj)) continue;
                    if (!sObj.TryGetProperty("u", out var uNode)) continue;

                    string? u = Normalize(uNode.GetString());
                    u = ConvertPreviewToDirect(u);

                    if (IsImageUrl(u))
                    {
                        yield return new RedditPostDto
                        {
                            Id = kv.Name,
                            Title = title,
                            Url = u!,
                            IsImage = true
                        };
                    }
                }
            }
        }

        // --- 以下函数严格复刻旧逻辑 ---

        private static string? TryGetMainUrl(JsonElement post)
        {
            if (post.TryGetProperty("url_overridden_by_dest", out var uNode))
                return Normalize(uNode.GetString());

            return null;
        }

        private static bool IsImageUrl(string? url)
        {
            if (string.IsNullOrWhiteSpace(url)) return false;

            return url.Contains("i.redd.it", StringComparison.OrdinalIgnoreCase) &&
                   (url.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ||
                    url.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase) ||
                    url.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ||
                    url.EndsWith(".webp", StringComparison.OrdinalIgnoreCase));
        }

        private static string Normalize(string? url)
        {
            if (string.IsNullOrEmpty(url)) return "";

            url = url.Replace("&amp;", "&");

            // 旧逻辑：忽略 gifv、redditmedia.com
            if (url.Contains("gifv", StringComparison.OrdinalIgnoreCase) ||
                url.EndsWith(".gifv", StringComparison.OrdinalIgnoreCase) ||
                url.Contains("redditmedia.com", StringComparison.OrdinalIgnoreCase))
                return "";

            return url;
        }

        private static string? ConvertPreviewToDirect(string? url)
        {
            if (string.IsNullOrEmpty(url)) return null;

            if (url.Contains("i.redd.it"))
                return url;

            if (url.Contains("preview.redd.it") ||
                url.Contains("external-preview.redd.it"))
            {
                // 1) 带扩展名
                var m1 = IdExtRegex.Match(url);
                if (m1.Success)
                    return $"https://i.redd.it/{m1.Groups[1].Value}.{m1.Groups[2].Value}";

                // 2) 仅 ID
                var m2 = IdOnlyRegex.Match(url);
                if (m2.Success)
                {
                    string id = m2.Groups[1].Value;

                    var f = FormatParamRegex.Match(url);
                    string ext = f.Success ? f.Groups[1].Value : "jpg";

                    return $"https://i.redd.it/{id}.{ext}";
                }
            }

            return url;
        }

        private static string ExtractIdFromUrl(string url)
        {
            var m = IdOnlyRegex.Match(url);
            return m.Success ? m.Groups[1].Value : "unknown";
        }
    }
}
