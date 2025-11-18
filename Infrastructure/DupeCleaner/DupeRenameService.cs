using RedgifsDownloader.Domain.Interfaces;
using System.IO;
using System.Text.RegularExpressions;

namespace RedgifsDownloader.Infrastructure.DupeCleaner
{
    public class DupeRenameService : IRenameService
    {
        public async Task<(int renamed, List<string> logs)> RenameAsync(string folderPath)
        {
            if (string.IsNullOrWhiteSpace(folderPath) || !Directory.Exists(folderPath))
                throw new DirectoryNotFoundException($"目录不存在: {folderPath}");

            var logs = new List<string>();
            int count = 0;
            var regex = new Regex(@"(S\d+__)+", RegexOptions.IgnoreCase);

            await Task.Run(() =>
            {
                foreach (var file in Directory.GetFiles(folderPath))
                {
                    string fileName = Path.GetFileName(file);
                    string newName = regex.Replace(fileName, "");

                    if (fileName == newName)
                        continue;

                    string newPath = Path.Combine(folderPath, newName);
                    if (File.Exists(newPath))
                    {
                        string name = Path.GetFileNameWithoutExtension(newName);
                        string ext = Path.GetExtension(newName);
                        newPath = Path.Combine(folderPath, $"{name}_renamed{ext}");
                    }

                    File.Move(file, newPath);
                    logs.Add($"{fileName} → {Path.GetFileName(newPath)}");
                    count++;
                }
            });

            return (count, logs);
        }
    }
}
