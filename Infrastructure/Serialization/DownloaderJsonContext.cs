using System.Text.Json.Serialization;
using RedgifsDownloader.Infrastructure.Redgifs;

namespace RedgifsDownloader.Infrastructure.Serialization;

[JsonSourceGenerationOptions(PropertyNameCaseInsensitive = true)]
[JsonSerializable(typeof(RedgifsPageResponse))]
[JsonSerializable(typeof(RedditTokenCache))]
internal partial class DownloaderJsonContext : JsonSerializerContext
{
}

internal sealed class RedditTokenCache
{
    [JsonPropertyName("refresh")]
    public string Refresh { get; init; } = string.Empty;

    [JsonPropertyName("expires")]
    public DateTime Expires { get; init; }
}
