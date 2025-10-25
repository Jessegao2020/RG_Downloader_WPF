using System.Diagnostics;
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

        public bool Exists(VideoItem video, string baseDir)
        {
            string videoPath = GetVideoPath(video, baseDir);
            if (!File.Exists(videoPath))
                return false;

            var info = new FileInfo(videoPath);
            Debug.WriteLine($"[EXISTS] Checking {video.Id}");
            Debug.WriteLine($"         Local: {info.Length} bytes");
            Debug.WriteLine($"         Expected: {video.ExpectedSize} bytes");
            if (video.ExpectedSize <= 0)
            {
                Debug.WriteLine($"[EXISTS] ⚠ ExpectedSize <= 0 → returning false");
                return false;
            }

            bool match = info.Length == video.ExpectedSize;
            Debug.WriteLine($"[EXISTS] Result: {(match ? "✅ MATCH" : "❌ MISMATCH")}");
            return match;
        }
    }
}
