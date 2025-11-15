using System.IO;
using RedgifsDownloader.Domain.Interfaces;

namespace RedgifsDownloader.Infrastructure.Settings
{
    public class FileStorage : IFileStorage
    {
        public string CombinePath(params string[] segments) => Path.Combine(segments);

        public void CreateDirectory(string path)
        {
            Directory.CreateDirectory(path);
        }

        public bool FileExists(string path) => File.Exists(path);

        public string GetBaseDirectory() => AppContext.BaseDirectory;

        public long GetFileSize(string path) => new FileInfo(path).Length;
    }
}
