using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using RedgifsDownloader.Model.Reddit;

namespace RedgifsDownloader.Helpers
{
    public static class RedditPostParser
    {
        public static List<RedditPost> ExtractImagePosts(string json)
        {
            var results = new List<RedditPost>();
            using var doc = JsonDocument.Parse(json);

            if (!doc.RootElement.TryGetProperty("data", out var dataNode) ||
                !dataNode.TryGetProperty("children", out var children))
                return results;

            foreach (var child in children.EnumerateArray())
            {
                var data = child.GetProperty("data");
                string title = data.TryGetProperty("title", out var titleProp)
                    ? titleProp.GetString() ?? ""
                    : "";

                // 1. 单图链接
                if (data.TryGetProperty("url_overridden_by_dest", out var urlProp))
                {
                    var url = urlProp.GetString();
                    if (IsImageUrl(url))
                        results.Add(new RedditPost { Title = title, Url = Normalize(url), IsImage = true });
                }

                // 2. preview.images
                if (data.TryGetProperty("preview", out var preview) &&
                    preview.TryGetProperty("images", out var images))
                {
                    foreach (var img in images.EnumerateArray())
                    {
                        var src = img.GetProperty("source").GetProperty("url").GetString();
                        results.Add(new RedditPost { Title = title, Url = Normalize(src), IsImage = true });
                    }
                }

                // 3. media_metadata（多图相册）
                if (data.TryGetProperty("media_metadata", out var mediaMeta))
                {
                    foreach (var kv in mediaMeta.EnumerateObject())
                    {
                        if (kv.Value.TryGetProperty("s", out var sObj) &&
                            sObj.TryGetProperty("u", out var urlNode))
                        {
                            results.Add(new RedditPost
                            {
                                Title = title,
                                Url = Normalize(urlNode.GetString()),
                                IsImage = true
                            });
                        }
                    }
                }
            }

            // 去重（按 URL）
            return results.GroupBy(r => r.Url)
                          .Select(g => g.First())
                          .ToList();
        }

        private static bool IsImageUrl(string? url) =>
             !string.IsNullOrEmpty(url) &&
             (url.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase)
             || url.EndsWith(".png", StringComparison.OrdinalIgnoreCase)
             || url.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase)
             || url.EndsWith(".gif", StringComparison.OrdinalIgnoreCase)
             || url.Contains("preview.redd.it"));

        private static string Normalize(string? url)
        {
            if (string.IsNullOrEmpty(url))
                return "";

            url = url.Replace("&amp;", "&");

            if (url.Contains("external-preview.redd.it"))
                return "";

            if (url.Contains("preview.redd.it"))
                url = url.Replace("preview.redd.it", "i.redd.it");

            return url;
        }
    }
}
