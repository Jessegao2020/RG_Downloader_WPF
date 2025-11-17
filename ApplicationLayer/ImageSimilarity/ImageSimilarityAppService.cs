using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RedgifsDownloader.Domain.Interfaces;

namespace RedgifsDownloader.ApplicationLayer.ImageSimilarity
{
    internal class ImageSimilarityAppService : IImageSimilarityAppService
    {
        private readonly IImageHashService _hashService;

        public ImageSimilarityAppService(IImageHashService hashService)
        {
            _hashService = hashService;
        }

        public Dictionary<string, (ulong normal, ulong flipped)> GetImageHashes(string folder, IProgress<(int done, int total)>? progress = null, Action<string>? log = null)
        {
            var dict = new ConcurrentDictionary<string, (ulong, ulong)>(StringComparer.OrdinalIgnoreCase);
            var exts = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            { ".jpg", ".jpeg", ".png", ".bmp", ".webp", ".heic" };

            var files = Directory.EnumerateFiles(folder, "*.*", SearchOption.TopDirectoryOnly)
                                 .Where(f => exts.Contains(Path.GetExtension(f)))
                                 .ToList();

            int total = files.Count, done = 0;
            log?.Invoke($"正在加载与计算哈希，共 {total} 张图片...");

            double totalLoad = 0, totalResize = 0, totalDct = 0;
            var locker = new object();

            Parallel.ForEach(
                files,
                new ParallelOptions
                {
                    MaxDegreeOfParallelism = Math.Max(1, Environment.ProcessorCount - 2)
                },
                file =>
                {
                    try
                    {
                        var (normal, flipped, times) = _hashService.ComputeHash(file);
                        dict[file] = (normal, flipped);

                        lock (locker)
                        {
                            totalLoad += times.loadMs;
                            totalResize += times.resizeMs;
                            totalDct += times.dctMs;
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[WARN] 无法处理文件 {file}: {ex.Message}");
                    }
                    finally
                    {
                        int current = Interlocked.Increment(ref done);
                        if (current % 5 == 0 || current == total)
                            progress?.Report((current, total));
                    }
                });

            // 和原来一样，最后 ToDictionary 一下
            return dict.ToDictionary(kv => kv.Key, kv => kv.Value, StringComparer.OrdinalIgnoreCase);
        }

        public Dictionary<int, List<string>> GroupSimilarImages(Dictionary<string, (ulong normal, ulong flipped)> hashes, double threshold)
        {
            var simGroups = new Dictionary<int, List<string>>();
            int groupId = 0;
            var remaining = new Dictionary<string, (ulong, ulong)>(hashes);

            while (remaining.Count > 0)
            {
                var e = remaining.GetEnumerator();
                e.MoveNext();
                var baseImg = e.Current.Key;
                var (baseHash, baseFlip) = e.Current.Value;
                remaining.Remove(baseImg);

                var group = new List<string> { baseImg };
                var toRemove = new List<string>();

                foreach (var kv in remaining)
                {
                    var (h, hFlip) = kv.Value;
                    double sim = Math.Max(
                        _hashService.Compare(baseHash, h),
                        _hashService.Compare(baseHash, hFlip));

                    if (sim >= threshold)
                    {
                        group.Add(kv.Key);
                        toRemove.Add(kv.Key);
                    }
                }

                foreach (var r in toRemove)
                    remaining.Remove(r);

                if (group.Count > 1)
                    simGroups[++groupId] = group;
            }

            return simGroups;
        }
    }
}
