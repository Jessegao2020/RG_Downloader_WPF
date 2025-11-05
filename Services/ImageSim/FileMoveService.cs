using System.IO;

namespace RedgifsDownloader.Services.ImageSim
{
    public class FileMoveService
    {
        public static void MoveToDupeFolder(string baseFolder, Dictionary<int, List<string>> groups)
        {
            string dupeDir = Path.Combine(baseFolder, "dupe");
            Directory.CreateDirectory(dupeDir);

            // 1️⃣ 找出当前 dupe 文件夹中最大的 S 编号
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
