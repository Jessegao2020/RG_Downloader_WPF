using RedgifsDownloader.Domain.Enums;

namespace RedgifsDownloader.Domain.Entities
{
    public class Video
    {
        public event Action? Onchanged;
        public string Id { get; }
        public string Username { get; }
        public Uri Url { get; }
        public string? Token { get; }
        public MediaPlatform Platform { get; }
        public long? CreateDateRaw { get; private set; }
        public double? Progress { get; private set; }
        public bool IsFailed { get; private set; }

        public VideoStatus Status { get; private set; }

        public Video(string id, string username, Uri url, MediaPlatform platform, string? token = null, long? createDateRaw = null)
        {
            Id = id;
            Username = username;
            Url = url;
            Platform = platform;
            CreateDateRaw = createDateRaw;
            Token = token;
            Status = VideoStatus.Pending;
        }

        public void SetProgress(double? p) { Console.WriteLine($"video setprogress({p}) called"); Progress = p; Onchanged?.Invoke(); }
        public void MarkDownloading() { Status = VideoStatus.Downloading; Onchanged?.Invoke(); }
        public void MarkCompleted() { Status = VideoStatus.Completed; IsFailed = false; Onchanged?.Invoke(); }
        public void MarkExists() { Status = VideoStatus.Exists; Onchanged?.Invoke(); }
        public void MarkFailed() { Status = VideoStatus.Failed; IsFailed = true; Onchanged?.Invoke(); }
        public void MarkCanceled() { Status = VideoStatus.Canceled; IsFailed = true; Onchanged?.Invoke(); }
        public void MarkNetworkError() { Status = VideoStatus.NetworkError; IsFailed = true; Onchanged?.Invoke(); }
        public void MarkWriteError() { Status = VideoStatus.WriteError; IsFailed = true; Onchanged?.Invoke(); }
        public void MarkUnknownError() { Status = VideoStatus.UnknownError; IsFailed = true; Onchanged?.Invoke(); }
    }
}
