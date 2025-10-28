using System.Text.Json;
using RedgifsDownloader.Model.Reddit;

namespace RedgifsDownloader.Helpers
{
    public static class RedditPostParser
    {
        public static List<RedditPost> ExtractImagePosts(JsonElement dataNode)
        {
            var results = new List<RedditPost>();

            if (!dataNode.TryGetProperty("children", out var children))
                return results;

            foreach (var child in children.EnumerateArray())
            {
                if (!child.TryGetProperty("data", out var data) || data.ValueKind != JsonValueKind.Object)
                    continue;

                string title = data.TryGetProperty("title", out var titleProp) && titleProp.ValueKind == JsonValueKind.String
                    ? titleProp.GetString() ?? ""
                    : "";

                // 1️⃣ 从 url_overridden_by_dest 获取主链接
                string? url = null;
                if (data.TryGetProperty("url_overridden_by_dest", out var urlProp) &&
                    urlProp.ValueKind == JsonValueKind.String)
                {
                    url = Normalize(urlProp.GetString());
                }

                // 2️⃣ 如果 URL 是 preview 或 external-preview，尝试转为 i.redd.it
                if (!string.IsNullOrEmpty(url))
                {
                    if (url.Contains("preview.redd.it", StringComparison.OrdinalIgnoreCase) ||
                        url.Contains("external-preview.redd.it", StringComparison.OrdinalIgnoreCase))
                    {
                        // 例如 https://preview.redd.it/abcd1234xyz.png?... → https://i.redd.it/abcd1234xyz.jpg
                        var match = System.Text.RegularExpressions.Regex.Match(url, @"redd\.it/([A-Za-z0-9]+)");
                        if (match.Success)
                            url = $"https://i.redd.it/{match.Groups[1].Value}.jpg";
                        else
                            url = null; // 无法转换，直接丢弃
                    }
                }

                // 3️⃣ 相册 media_metadata 中的图片
                if (data.TryGetProperty("media_metadata", out var mediaMeta) &&
                    mediaMeta.ValueKind == JsonValueKind.Object)
                {
                    foreach (var kv in mediaMeta.EnumerateObject())
                    {
                        if (kv.Value.TryGetProperty("s", out var sObj) &&
                            sObj.ValueKind == JsonValueKind.Object &&
                            sObj.TryGetProperty("u", out var uNode) &&
                            uNode.ValueKind == JsonValueKind.String)
                        {
                            var u = Normalize(uNode.GetString());
                            if (u.Contains("preview.redd.it", StringComparison.OrdinalIgnoreCase))
                            {
                                var match = System.Text.RegularExpressions.Regex.Match(u, @"redd\.it/([A-Za-z0-9]+)");
                                if (match.Success)
                                    u = $"https://i.redd.it/{match.Groups[1].Value}.jpg";
                                else
                                    continue;
                            }

                            if (IsImageUrl(u))
                                results.Add(new RedditPost { Title = title, Url = u, IsImage = true });
                        }
                    }
                }
                else if (IsImageUrl(url))
                {
                    results.Add(new RedditPost { Title = title, Url = url!, IsImage = true });
                }
            }

            // 4️⃣ 去重，只保留 i.redd.it 域名
            return results
                .Where(r => !string.IsNullOrWhiteSpace(r.Url)
                         && r.Url.Contains("i.redd.it", StringComparison.OrdinalIgnoreCase))
                .GroupBy(r => r.Url)
                .Select(g => g.First())
                .ToList();
        }

        private static bool IsImageUrl(string? url) =>
            !string.IsNullOrEmpty(url) &&
            url.Contains("i.redd.it", StringComparison.OrdinalIgnoreCase) &&
            (url.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase)
             || url.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase)
             || url.EndsWith(".png", StringComparison.OrdinalIgnoreCase));


        private static string Normalize(string? url)
        {
            if (string.IsNullOrEmpty(url))
                return "";

            url = url.Replace("&amp;", "&");

            // 忽略 Reddit 外链 / GIF / 视频封面
            if (url.Contains("gifv", StringComparison.OrdinalIgnoreCase) ||
                url.EndsWith(".gif", StringComparison.OrdinalIgnoreCase) ||
                url.Contains("external-preview.redd.it", StringComparison.OrdinalIgnoreCase) ||
                url.Contains("redditmedia.com", StringComparison.OrdinalIgnoreCase))
                return "";

            return url;
        }
    }
}
