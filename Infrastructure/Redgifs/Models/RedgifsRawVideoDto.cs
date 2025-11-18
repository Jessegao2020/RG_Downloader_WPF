namespace RedgifsDownloader.Infrastructure.Redgifs.Models
{
    public class RedgifsRawVideoDto
    {
        public string Id { get; set; }
        public string Url { get; set; }
        public string? Username { get; set; }
        public long? CreateDateRaw { get; set; }
        public string Token { get; set; }

    }
}
