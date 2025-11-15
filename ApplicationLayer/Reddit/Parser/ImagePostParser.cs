using System.Text.Json;
using System.Text.RegularExpressions;
using RedgifsDownloader.ApplicationLayer.DTOs;

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

            // ------------- 1) 先尝试直链 / preview 链接 -------------
            string? url = TryGetMainUrl(post);
            url = ConvertPreviewToDirect(url);

            // 直接图（i.redd.it）
            if (IsImageUrl(url))
            {
                yield return new RedditPostDto
                {
                    Id = ExtractIdFromUrl(url!),
                    Title = title,
                    Url = url!,
                    IsImage = true,
                };
                yield break;
            }

            // ------------- 2) 相册 media_metadata -------------
            if (post.TryGetProperty("media_metadata", out var mediaMeta)
                && mediaMeta.ValueKind == JsonValueKind.Object)
            {
                foreach (var kv in mediaMeta.EnumerateObject())
                {
                    if (!kv.Value.TryGetProperty("s", out var sObj))
                        continue;

                    if (!sObj.TryGetProperty("u", out var uNode))
                        continue;

                    var u = ConvertPreviewToDirect(Normalize(uNode.GetString()));

                    if (IsImageUrl(u))
                    {
                        yield return new RedditPostDto
                        {
                            Id = kv.Name,
                            Title = title,
                            Url = u!,
                            IsImage = true,
                        };
                    }
                }
            }
        }

        private static string? TryGetMainUrl(JsonElement post)
        {
            if (post.TryGetProperty("url_overridden_by_dest", out var urlProp))
                return Normalize(urlProp.GetString());

            return null;
        }

        private static bool IsImageUrl(string? url)
            => url != null && url.Contains("i.redd.it") &&
               (url.EndsWith(".jpg") || url.EndsWith(".png") || url.EndsWith(".webp"));

        private static string Normalize(string? url)
        {
            if (string.IsNullOrEmpty(url)) return "";
            return url.Replace("&amp;", "&");
        }

        private static string? ConvertPreviewToDirect(string? url)
        {
            if (string.IsNullOrEmpty(url)) return null;

            // 直链
            if (url.Contains("i.redd.it")) return url;

            // preview 链接
            if (url.Contains("preview.redd.it"))
            {
                var m1 = IdExtRegex.Match(url);
                if (m1.Success)
                    return $"https://i.redd.it/{m1.Groups[1].Value}.{m1.Groups[2].Value}";

                var m2 = IdOnlyRegex.Match(url);
                if (m2.Success)
                    return $"https://i.redd.it/{m2.Groups[1].Value}.jpg";
            }

            return url;
        }

        private static string ExtractIdFromUrl(string url)
        {
            var match = IdOnlyRegex.Match(url);
            return match.Success ? match.Groups[1].Value : "unknown";
        }
    }
}
