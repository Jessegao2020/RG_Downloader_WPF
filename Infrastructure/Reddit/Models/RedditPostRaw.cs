namespace RedgifsDownloader.Infrastructure.Reddit.Models
{
    public class RedditPostRaw
    {
        public string Title { get; set; }
        public string Id { get; set; }
        public string Url { get; set; }
        public bool IsImage { get; set; }
    }
}
