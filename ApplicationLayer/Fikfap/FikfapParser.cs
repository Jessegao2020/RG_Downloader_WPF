using System.Text.Json;
using RedgifsDownloader.ApplicationLayer.DTOs;

namespace RedgifsDownloader.ApplicationLayer.Fikfap
{
    public class FikfapParser
    {
        public static IEnumerable<VideoDto> Extract(string json)
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (root.ValueKind != JsonValueKind.Array)
                yield break;
                        
            foreach (var p in root.EnumerateArray())
            {
                if (!p.TryGetProperty("videoStreamUrl", out var urlNode) || urlNode.ValueKind != JsonValueKind.String)
                    continue;

                string videoUrl = urlNode.GetString() ?? "";
                if (string.IsNullOrWhiteSpace(videoUrl))
                    continue;

                string username = "";
                if (p.TryGetProperty("author", out var author) &&
                        author.TryGetProperty("username", out var uNode) &&
                        uNode.ValueKind == JsonValueKind.String)
                {
                    username = uNode.GetString() ?? "";
                }

                string mediaId = p.TryGetProperty("mediaId", out var mid) && 
                    mid.ValueKind == JsonValueKind.String ? mid.GetString() ?? "" : "";

                string createAt = p.TryGetProperty("createdAt", out var createdNode) && createdNode.ValueKind == JsonValueKind.String
                    ? createdNode.GetString() ?? "" : "";

                yield return new VideoDto
                {
                    Username = username,
                    Id = mediaId,
                    Url = videoUrl,
                    CreateDateRaw = DateTimeOffset.Parse(createAt).ToUnixTimeSeconds()
                };
            }
        }
    }
}
