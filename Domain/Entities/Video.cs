using RedgifsDownloader.Domain.Enums;

namespace RedgifsDownloader.Domain.Entities
{
    public class Video
    {
        public string Id { get; }
        public string Username { get; }
        public Uri Url { get; }
        public MediaPlatform Platform { get; }

        public VideoStatus Status { get; private set; }

        public Video(string id, string username, Uri url, MediaPlatform platform)
        {
            Id = id;
            Username = username;
            Url = url;
            Platform = platform;
            Status = VideoStatus.Pending;
        }

        public void MarkDownloading() => Status = VideoStatus.Downloading;
        public void MarkCompleted() => Status = VideoStatus.Completed;
        public void MarkFailed() => Status = VideoStatus.Failed;
        public bool IsFailed() => Status == VideoStatus.Failed;
    }
}
