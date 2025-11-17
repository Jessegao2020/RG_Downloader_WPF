using System.Text.Json;

namespace RedgifsDownloader.ApplicationLayer.Reddit.Parser
{
    public class RedditPostParser
    {
        public static IEnumerable<JsonElement> EnumerateChildren(JsonDocument doc)
        {
            if (!doc.RootElement.TryGetProperty("data", out var data))
                yield break;

            if(!data.TryGetProperty("children", out var children))
                    yield break;

            foreach (var child in children.EnumerateArray())
            {
                if (child.TryGetProperty("data", out var post))
                    yield return post;
            }
        }
    }
}
