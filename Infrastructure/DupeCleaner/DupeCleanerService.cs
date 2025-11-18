using RedgifsDownloader.Domain.Interfaces;
using System.IO;
using System.Text.RegularExpressions;

namespace RedgifsDownloader.Infrastructure.DupeCleaner
{
    public class DupeCleanerService : IDupeCleanerService
    {
        private readonly Regex _regex = new(@"^S(\d+)__");

        public async Task<(int kept, int deleted, List<string> logs)> CleanAsync(string folder)
        {
            return await Task.Run(() =>
            {
                var files = Directory.GetFiles(folder);
                var groups = new Dictionary<int, List<FileInfo>>();
                var logs = new List<string>();

                foreach (var path in files)
                {
                    var name = Path.GetFileName(path);
                    var match = _regex.Match(name);
                    if (!match.Success) continue;

                    int groupId = int.Parse(match.Groups[1].Value);
                    if (!groups.ContainsKey(groupId))
                        groups[groupId] = new List<FileInfo>();
                    groups[groupId].Add(new FileInfo(path));
                }

                int deleted = 0, kept = 0;

                foreach (var kvp in groups)
                {
                    var list = kvp.Value;
                    if (list.Count < 2) continue;

                    var keep = list.OrderByDescending(f => f.Length).First();
                    foreach (var file in list)
                    {
                        if (file.FullName == keep.FullName) continue;

                        try
                        {
                            file.Delete();
                            deleted++;
                        }
                        catch (Exception ex)
                        {
                            logs.Add($"删除失败 {file.Name}: {ex.Message}");
                        }
                    }

                    kept++;
                    logs.Add($"S{kvp.Key:D3} 保留 {keep.Name}");
                }

                logs.Add($"\n共保留 {kept} 组，删除 {deleted} 个重复文件。");
                return (kept, deleted, logs);
            });
        }
    }
}
