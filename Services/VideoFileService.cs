using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Shapes;

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

        public bool Exists(VideoItem video, string baseDir) => File.Exists(GetVideoPath(video, baseDir));
    }
}
