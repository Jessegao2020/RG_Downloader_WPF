using RedgifsDownloader.Model.Reddit;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace RedgifsDownloader.Helpers
{
    public static class RedditPostParser
    {
        private static readonly Regex IdExtRegex =
            new(@"redd\.it/([A-Za-z0-9]+)\.(jpg|jpeg|png|gif|webp)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex IdOnlyRegex =
            new(@"redd\.it/([A-Za-z0-9]+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex FormatParamRegex =
            new(@"format=(png|jpg|jpeg|gif|webp)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

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
                    url = ConvertPreviewToDirect(url);
                }

                // 2️⃣ 相册 media_metadata 中的图片
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
                            u = ConvertPreviewToDirect(u);

                            if (IsImageUrl(u))
                                results.Add(new RedditPost { Title = title, Id = kv.Name, Url = u, IsImage = true });
                        }
                    }
                }
                else if (IsImageUrl(url))
                {
                    string id = ExtractIdFromUrl(url);
                    results.Add(new RedditPost { Title = title, Id = id, Url = url!, IsImage = true });
                }
            }

            // 3️⃣ 去重，只保留 i.redd.it 域名
            return results
                .Where(r => !string.IsNullOrWhiteSpace(r.Url)
                         && r.Url.Contains("i.redd.it", StringComparison.OrdinalIgnoreCase))
                .GroupBy(r => r.Url)
                .Select(g => g.First())
                .ToList();
        }

        /// <summary>
        /// 将 preview.redd.it/external-preview.redd.it 链接转换为 i.redd.it 原图直链
        /// </summary>
        private static string? ConvertPreviewToDirect(string? url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return null;

            // 已经是直链就不处理
            if (url.Contains("i.redd.it", StringComparison.OrdinalIgnoreCase))
                return url;

            if (url.Contains("preview.redd.it", StringComparison.OrdinalIgnoreCase) ||
                url.Contains("external-preview.redd.it", StringComparison.OrdinalIgnoreCase))
            {
                // 先尝试带扩展名匹配
                var m1 = IdExtRegex.Match(url);
                if (m1.Success)
                    return $"https://i.redd.it/{m1.Groups[1].Value}.{m1.Groups[2].Value}";

                // 再看是否有 format 参数
                var idMatch = IdOnlyRegex.Match(url);
                if (idMatch.Success)
                {
                    string id = idMatch.Groups[1].Value;
                    var formatMatch = FormatParamRegex.Match(url);
                    string ext = formatMatch.Success ? formatMatch.Groups[1].Value : "jpg";
                    return $"https://i.redd.it/{id}.{ext}";
                }
            }

            return url;
        }

        private static bool IsImageUrl(string? url) =>
            !string.IsNullOrEmpty(url) &&
            url.Contains("i.redd.it", StringComparison.OrdinalIgnoreCase) &&
            (url.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase)
             || url.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase)
             || url.EndsWith(".png", StringComparison.OrdinalIgnoreCase)
             || url.EndsWith(".gif", StringComparison.OrdinalIgnoreCase)
             || url.EndsWith(".webp", StringComparison.OrdinalIgnoreCase));

        private static string Normalize(string? url)
        {
            if (string.IsNullOrEmpty(url))
                return "";

            url = url.Replace("&amp;", "&");

            // 忽略 GIFV、外部预览和视频封面
            if (url.Contains("gifv", StringComparison.OrdinalIgnoreCase) ||
                url.EndsWith(".gifv", StringComparison.OrdinalIgnoreCase) ||
                url.Contains("redditmedia.com", StringComparison.OrdinalIgnoreCase))
                return "";

            return url;
        }

        private static string ExtractIdFromUrl(string url)
        {
            // 匹配 i.redd.it/xxxxx.jpg 中的 xxxxx
            var match = Regex.Match(url, @"redd\.it/([A-Za-z0-9]+)", RegexOptions.IgnoreCase);
            return match.Success ? match.Groups[1].Value : "unknown";
        }
    }
}
