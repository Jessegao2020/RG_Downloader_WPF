using System.Text.Json;

namespace RedgifsDownloader.ApplicationLayer.Reddit.Parser
{
    public class RedditPostParser
    {
        public static IEnumerable<JsonElement> EnumerateChildren(JsonDocument doc)
        {
            var data = doc.RootElement.GetProperty("data");

            if (!data.TryGetProperty("children", out var children))
                yield break;

            foreach (var child in children.EnumerateArray())
            {
                if (child.TryGetProperty("data", out var post))
                    yield return post;
            }
        }
    }
}
