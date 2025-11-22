using RedgifsDownloader.Domain.Enums;

namespace RedgifsDownloader.Domain.Entities
{
    public class Video
    {
        private int _version;

        public string Id { get; }
        public string Username { get; }
        public Uri Url { get; }
        public string? Token { get; }
        public MediaPlatform Platform { get; }
        public long? CreateDateRaw { get; private set; }
        public double? Progress { get; private set; }
        public bool IsFailed { get; private set; }
        public string? ThumbnailUrl { get; }

        public VideoStatus Status { get; private set; }

        // 内部版本号，用于 ViewModel 检测变化
        public int Version => _version;

        public Video(string id, string username, Uri url, MediaPlatform platform, string? token = null, long? createDateRaw = null, string? thumbnailUrl = null)
        {
            Id = id;
            Username = username;
            Url = url;
            Platform = platform;
            CreateDateRaw = createDateRaw;
            Token = token;
            ThumbnailUrl = thumbnailUrl;
            Status = VideoStatus.Pending;
        }

        #region 设置状态变化
        public void SetProgress(double? p)
        {
            Progress = p;
            _version++;
        }

        public void MarkDownloading()
        {
            Status = VideoStatus.Downloading;
            _version++;
        }

        public void MarkCompleted()
        {
            Status = VideoStatus.Completed;
            IsFailed = false;
            _version++;
        }

        public void MarkExists()
        {
            Status = VideoStatus.Exists;
            _version++;
        }

        public void MarkFailed()
        {
            Status = VideoStatus.Failed;
            IsFailed = true;
            _version++;
        }

        public void MarkCanceled()
        {
            Status = VideoStatus.Canceled;
            IsFailed = true;
            _version++;
        }

        public void MarkNetworkError()
        {
            Status = VideoStatus.NetworkError;
            IsFailed = true;
            _version++;
        }

        public void MarkWriteError()
        {
            Status = VideoStatus.WriteError;
            IsFailed = true;
            _version++;
        }

        public void MarkUnknownError()
        {
            Status = VideoStatus.UnknownError;
            IsFailed = true;
            _version++;
        }
        #endregion
    }
}
