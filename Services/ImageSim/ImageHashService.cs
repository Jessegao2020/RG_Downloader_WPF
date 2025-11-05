using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Drawing;
using System.IO;

namespace RedgifsDownloader.Services.ImageSim
{
    public class ImageHashService
    {
        public static ulong ComputeHash(string path)
        {
            using var image = SixLabors.ImageSharp.Image.Load<Rgba32>(path);
            image.Mutate(x => x.Resize(8, 8).Grayscale());

            var gray = new byte[64];
            double sum = 0;
            int i = 0;

            image.ProcessPixelRows(accessor =>
            {
                for (int y = 0; y < 8; y++)
                {
                    var row = accessor.GetRowSpan(y);
                    for (int x = 0; x < 8; x++)
                    {
                        var p = row[x];
                        byte v = (byte)(p.R * 0.299 + p.G * 0.587 + p.B * 0.114);
                        gray[i++] = v;
                        sum += v;
                    }
                }
            });

            double avg = sum / 64.0;
            ulong hash = 0UL;
            for (int j = 0; j < 64; j++)
            {
                if (gray[j] >= avg)
                    hash |= 1UL << j;
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
            var dict = new ConcurrentDictionary<string, ulong>(StringComparer.OrdinalIgnoreCase);
            var exts = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg", ".png", ".bmp", ".webp", ".heic" };

            var files = Directory.EnumerateFiles(folder, "*.*", SearchOption.TopDirectoryOnly)
                                 .Where(f => exts.Contains(Path.GetExtension(f)))
                                 .ToList();

            // 并行计算哈希
            Parallel.ForEach(files, new ParallelOptions{MaxDegreeOfParallelism = Environment.ProcessorCount},
            file =>
            {
                try
                {
                    dict[file] = ComputeHash(file);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[WARN] 无法处理文件 {file}: {ex.Message}");
                }
            });

            return dict.ToDictionary(kv => kv.Key, kv => kv.Value, StringComparer.OrdinalIgnoreCase);
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
