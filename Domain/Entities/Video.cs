using RedgifsDownloader.Domain.Enums;

namespace RedgifsDownloader.Domain.Entities
{
    public class Video
    {
        public string Id { get; }
        public string Username { get; }
        public Uri Url { get; }
        public MediaPlatform Platform { get; }
        public long? CreateDateRaw { get; private set; }
        public double? Progress { get; private set; }

        public VideoStatus Status { get; private set; }

        public Video(string id, string username, Uri url, MediaPlatform platform, long? createDateRaw = null)
        {
            Id = id;
            Username = username;
            Url = url;
            Platform = platform;
            CreateDateRaw = createDateRaw;
            Status = VideoStatus.Pending;
        }

        public void SetProgress(double? p) => Progress = p;
        public void MarkDownloading() => Status = VideoStatus.Downloading;
        public void MarkCompleted() => Status = VideoStatus.Completed;
        public void MarkFailed() => Status = VideoStatus.Failed;
        public bool IsFailed() => Status == VideoStatus.Failed;
    }
}
