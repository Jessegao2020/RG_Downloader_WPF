using RedgifsDownloader.Domain.Enums;

namespace RedgifsDownloader.ApplicationLayer.DTOs
{
    public class VideoDto
    {
        public string Id { get; set; } = "";
        public string Username { get; set; } = "";
        public string Url { get; set; } = "";
        public long? CreateDateRaw { get; set; }
        public string? Token { get; set; }
        public MediaPlatform Platform { get; set; }

        public VideoDto() { }

        public VideoDto(string id, string username, string url, string? token, long? createDataRaw, MediaPlatform platform)
        {
            Id = id;
            Username = username;
            Url = url;
            CreateDateRaw = createDataRaw;
            Token = token;
            Platform = platform;
        }
    }
}
