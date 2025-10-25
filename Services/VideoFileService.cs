using System.IO;
using RedgifsDownloader.Model;

namespace RedgifsDownloader.Services
{
    public class VideoFileService
    {
        public string EnsureDownloadBaseDirectory()
        {
            string appBaseDir = AppContext.BaseDirectory;
            string baseDir = System.IO.Path.Combine(appBaseDir, "Downloads");
            Directory.CreateDirectory(baseDir);
            return baseDir;
        }

        public string GetVideoPath(VideoItem video, string baseDir)
        {
            string userName = string.IsNullOrWhiteSpace(video.Username) ? "Unknown" : video.Username.Trim();
            string saveDir = System.IO.Path.Combine(baseDir, userName);
            Directory.CreateDirectory(saveDir);
            return System.IO.Path.Combine(saveDir, video.Id + ".mp4");
        }

        public bool Exists(VideoItem video, string baseDir, bool strictCheck = false)
        {
            string videoPath = GetVideoPath(video, baseDir);
            if (!File.Exists(videoPath))
                return false;

            if (!strictCheck)
                return true;

            var info = new FileInfo(videoPath);
            if (video.ExpectedSize <= 0)
                return false;

            return info.Length == video.ExpectedSize;
        }
    }
}
