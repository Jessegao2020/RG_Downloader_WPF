using System.Diagnostics;
using System.Drawing;
using System.IO;

namespace RedgifsDownloader.Services.ImageSim
{
    public class ImageHashService
    {
        public static ulong ComputeHash(string path)
        {
            using var bmp = new Bitmap(path);
            using var small = new Bitmap(8, 8);
            using (var g = Graphics.FromImage(small))
            {
                g.DrawImage(bmp, 0, 0, 8, 8);
            }

            // 转灰度并求平均
            var gray = new byte[64];
            double sum = 0;
            int idx = 0;
            for (int y = 0; y < 8; y++)
            {
                for (int x = 0; x < 8; x++)
                {
                    var pixel = small.GetPixel(x, y);
                    byte val = (byte)(pixel.R * 0.299 + pixel.G * 0.587 + pixel.B * 0.114);
                    gray[idx++] = val;
                    sum += val;
                }
            }

            double avg = sum / 64;
            ulong hash = 0;
            for (int i = 0; i < 64; i++)
            {
                if (gray[i] >= avg)
                    hash |= 1UL << i;
            }
            return hash;
        }

        public static double Compare(ulong a, ulong b)
        {
            ulong x = a ^ b;
            int diff = 0;
            while (x != 0)
            {
                diff++;
                x &= x - 1;
            }
            return 1.0 - diff / 64.0;
        }

        public static Dictionary<string, ulong> GetImageHashes(string folder)
        {
            var dict = new Dictionary<string, ulong>(StringComparer.OrdinalIgnoreCase);
            var exts = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            { ".jpg", ".jpeg", ".png", ".bmp", ".webp", ".heic" };

            foreach (var file in Directory.EnumerateFiles(folder, "*.*", SearchOption.TopDirectoryOnly))
            {
                if (!exts.Contains(Path.GetExtension(file)))
                    continue;
                try
                {
                    dict[file] = ComputeHash(file);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[WARN] 无法处理文件 {file}: {ex.Message}");
                }
            }
            return dict;
        }

        public static Dictionary<int, List<string>> GroupSimilarImages(
            Dictionary<string, ulong> hashes, double threshold)
        {
            var simGroups = new Dictionary<int, List<string>>();
            int groupId = 0;
            var remaining = new Dictionary<string, ulong>(hashes);

            while (remaining.Count > 0)
            {
                var e = remaining.GetEnumerator();
                e.MoveNext();
                var baseImg = e.Current.Key;
                var baseHash = e.Current.Value;
                remaining.Remove(baseImg);

                var group = new List<string> { baseImg };
                var toRemove = new List<string>();

                foreach (var kv in remaining)
                {
                    double sim = Compare(baseHash, kv.Value);
                    if (sim >= threshold)
                    {
                        group.Add(kv.Key);
                        toRemove.Add(kv.Key);
                    }
                }

                foreach (var r in toRemove)
                    remaining.Remove(r);

                if (group.Count > 1)
                {
                    groupId++;
                    simGroups[groupId] = group;
                }
            }
            return simGroups;
        }
    }
}
