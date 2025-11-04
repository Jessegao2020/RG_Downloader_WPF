using System.IO;

namespace RedgifsDownloader.Services.ImageSim
{
    public class FileMoveService
    {
        public static void MoveToDupeFolder(string baseFolder, Dictionary<int, List<string>> groups)
        {
            string dupeDir = Path.Combine(baseFolder, "dupe");
            Directory.CreateDirectory(dupeDir);

            foreach (var kv in groups)
            {
                int gid = kv.Key;
                foreach (var src in kv.Value)
                {
                    string newName = $"S{gid}__{Path.GetFileName(src)}";
                    string dst = Path.Combine(dupeDir, newName);

                    if (File.Exists(dst))
                        File.Delete(dst);
                    File.Move(src, dst);
                }
            }
        }
    }
}
