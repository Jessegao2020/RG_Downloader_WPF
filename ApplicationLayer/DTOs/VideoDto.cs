using RedgifsDownloader.Domain.Enums;

namespace RedgifsDownloader.ApplicationLayer.DTOs
{
    public class VideoDto
    {
        public string Id { get; set; } = "";
        public string Username { get; set; } = "";
        public string Url { get; set; } = "";
        public MediaPlatform Platform { get; set; }

        public VideoDto() { }

        public VideoDto(string id, string username, string url, MediaPlatform platform)
        {
            Id = id;
            Username = username;
            Url = url;
            Platform = platform;
        }
    }
}
