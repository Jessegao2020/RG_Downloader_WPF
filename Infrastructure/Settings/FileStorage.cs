using RedgifsDownloader.Domain.Interfaces;
using System.IO;

namespace RedgifsDownloader.Infrastructure.Settings
{
    public class FileStorage : IFileStorage
    {
        public string CombinePath(params string[] segments) => Path.Combine(segments);

        public void CreateDirectory(string path) => Directory.CreateDirectory(path);

        public bool FileExists(string path) => File.Exists(path);

        public bool FileExistsWithCommonExtensions(string basePath)
        {
            if (File.Exists(basePath)) return true;

            // 常见多媒体后缀，统一在此管理
            var extensions = new[] { ".mp4", ".jpg", ".png", ".jpeg", ".gif", ".mkv", ".webm", ".ts", ".mov", ".avi", ".wmv" };
            foreach (var ext in extensions)
            {
                // 如果 basePath 结尾已经是扩展名，这里可能重复拼接，但在 File.Exists 语义下通常无害
                // 主要是为了解决传进来的是 "filename" 而实际存在 "filename.mp4" 的情况
                if (File.Exists(basePath + ext)) return true;
            }
            return false;
        }

        public string GetBaseDirectory() => AppContext.BaseDirectory;

        public long GetFileSize(string path) => new FileInfo(path).Length;
    }
}
