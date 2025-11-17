using System.IO;
using RedgifsDownloader.Domain.Interfaces;

namespace RedgifsDownloader.Infrastructure.ImageSim
{
    public class FileMoveService : IDupeFileMoveService
    {
        public void MoveToDupeFolder(string baseFolder, Dictionary<int, List<string>> groups)
        {
            string dupeDir = Path.Combine(baseFolder, "dupe");
            Directory.CreateDirectory(dupeDir);

            int nextGroupIndex = GetNextGroupStartIndex(dupeDir);

            foreach (var kv in groups)
            {
                int gid = nextGroupIndex++;
                foreach (var src in kv.Value)
                {
                    string newName = $"S{gid}__{Path.GetFileName(src)}";
                    string dst = Path.Combine(dupeDir, newName);

                    try
                    {
                        if (File.Exists(dst))
                            File.Delete(dst);
                        File.Move(src, dst);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[WARN] 无法移动 {src}: {ex.Message}");
                    }
                }
            }
        }

        private static int GetNextGroupStartIndex(string dupeFolder)
        {
            if (!Directory.Exists(dupeFolder))
                return 1;

            var existing = Directory.EnumerateFiles(dupeFolder)
                                    .Select(f => Path.GetFileName(f))
                                    .Where(name => name.StartsWith("S") && name.Contains("__"))
                                    .Select(name =>
                                    {
                                        var prefix = name.Split("__")[0].TrimStart('S');
                                        return int.TryParse(prefix, out int n) ? n : 0;
                                    })
                                    .DefaultIfEmpty(0);

            return existing.Max() + 1;
        }
    }
}
