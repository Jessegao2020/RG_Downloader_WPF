using RedgifsDownloader.Domain.Entities;

namespace RedgifsDownloader.ApplicationLayer.Downloads
{
    public class DownloadContext
    {
        public Video Video { get; }
        public long? ExpectedSize { get; set; }
        public double Progress { get; set; }
        public string FilePath { get; set; }

        public DownloadContext(Video video)
        {
            Video = video;
        }
    }
}
