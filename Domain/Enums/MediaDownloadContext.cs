namespace RedgifsDownloader.Domain.Enums
{
    public record MediaDownloadContext
    {
        public MediaPlatform Platform { get; init; }
        public string? Token { get; set; }
        public Dictionary<string, string>? Headers { get; init; }
    }
}
