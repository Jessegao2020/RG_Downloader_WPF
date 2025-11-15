namespace RedgifsDownloader.ApplicationLayer.DTOs
{
    public class RedditPostDto
    {
        public string Id { get; set; } = "";
        public string Title { get; set; } = "";
        public string Url { get; set; } = "";
        public bool IsImage { get; set; }
    }
}
