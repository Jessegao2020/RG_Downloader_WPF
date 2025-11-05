using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Numerics;

namespace RedgifsDownloader.Services.ImageSim
{
    public class ImageHashService
    {
        private const int N = 32;
        private const int K = 8;
        private static readonly float[,] Cos = BuildCos(N);
        private static readonly float[] Alpha = BuildAlpha(N);

        public static (ulong normal, ulong flipped, (double loadMs, double resizeMs, double dctMs)) ComputeHash(string path)
        {
            var sw = Stopwatch.StartNew();
            using var img = SixLabors.ImageSharp.Image.Load<Rgba32>(path);
            double loadMs = sw.Elapsed.TotalMilliseconds;

            sw.Restart();
            img.Mutate(x => x.Resize(N, N).Grayscale());
            double resizeMs = sw.Elapsed.TotalMilliseconds;

            // 转灰度矩阵
            var gray = new byte[N * N];
            img.ProcessPixelRows(accessor =>
            {
                for (int y = 0; y < N; y++)
                {
                    var row = accessor.GetRowSpan(y);
                    for (int x = 0; x < N; x++)
                    {
                        var p = row[x];
                        gray[y * N + x] = (byte)(p.R * 0.299 + p.G * 0.587 + p.B * 0.114);
                    }
                }
            });

            sw.Restart();
            ulong normal = ComputeHashFromGray(gray);
            ulong flipped = ComputeHashFromGray(FlipGray(gray, N));
            double dctMs = sw.Elapsed.TotalMilliseconds;

            return (normal, flipped, (loadMs, resizeMs, dctMs));
        }

        private static ulong ComputeHashFromGray(byte[] gray)
        {
            float[] input = new float[N * N];
            for (int i = 0; i < gray.Length; i++) input[i] = gray[i];

            float[] temp = new float[N * N];
            float[] dct = new float[N * N];

            // DCT 行
            for (int y = 0; y < N; y++)
            {
                int yBase = y * N;
                for (int u = 0; u < N; u++)
                {
                    float sum = 0f;
                    for (int x = 0; x < N; x++)
                        sum += input[yBase + x] * Cos[u, x];
                    temp[yBase + u] = Alpha[u] * sum;
                }
            }

            // DCT 列
            for (int u = 0; u < N; u++)
            {
                for (int v = 0; v < N; v++)
                {
                    float sum = 0f;
                    for (int y = 0; y < N; y++)
                        sum += temp[y * N + u] * Cos[v, y];
                    dct[v * N + u] = Alpha[v] * sum;
                }
            }

            Span<float> ac = stackalloc float[K * K];
            int k = 0;
            for (int v = 1; v <= K; v++)
            {
                int vBase = v * N;
                for (int u = 1; u <= K; u++)
                    ac[k++] = dct[vBase + u];
            }

            float median = Median64(ac);
            ulong hash = 0UL;
            for (int i = 0; i < ac.Length; i++)
                if (ac[i] > median) hash |= 1UL << i;
            return hash;
        }

        public static double Compare(ulong a, ulong b)
        {
            int diff = BitOperations.PopCount(a ^ b);
            return 1.0 - diff / 64.0;
        }

        public static Dictionary<string, (ulong normal, ulong flipped)> GetImageHashes(
            string folder, IProgress<(int done, int total)>? progress = null, Action<string>? log = null)
        {
            var dict = new ConcurrentDictionary<string, (ulong, ulong)>(StringComparer.OrdinalIgnoreCase);
            var exts = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg", ".png", ".bmp", ".webp", ".heic" };

            var files = Directory.EnumerateFiles(folder, "*.*", SearchOption.TopDirectoryOnly)
                                 .Where(f => exts.Contains(Path.GetExtension(f)))
                                 .ToList();

            int total = files.Count, done = 0;
            log?.Invoke($"正在加载与计算哈希，共 {total} 张图片...");
            double totalLoad = 0, totalResize = 0, totalDct = 0;
            var locker = new object();

            Parallel.ForEach(files, new ParallelOptions { MaxDegreeOfParallelism = Math.Max(1, Environment.ProcessorCount - 2) }, file =>
            {
                try
                {
                    var (normal, flipped, times) = ComputeHash(file);
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

            return dict.ToDictionary(kv => kv.Key, kv => kv.Value, StringComparer.OrdinalIgnoreCase);
        }

        public static Dictionary<int, List<string>> GroupSimilarImages(
            Dictionary<string, (ulong normal, ulong flipped)> hashes, double threshold)
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
                    double sim = Math.Max(Compare(baseHash, h), Compare(baseHash, hFlip));
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

        // --- Helpers ---
        private static byte[] FlipGray(byte[] src, int side)
        {
            var dst = new byte[src.Length];
            for (int y = 0; y < side; y++)
            {
                int baseIdx = y * side;
                for (int x = 0; x < side; x++)
                    dst[baseIdx + x] = src[baseIdx + (side - 1 - x)];
            }
            return dst;
        }

        private static float[,] BuildCos(int n)
        {
            var t = new float[n, n];
            for (int k = 0; k < n; k++)
                for (int i = 0; i < n; i++)
                    t[k, i] = (float)Math.Cos(((2 * i + 1) * k * Math.PI) / (2.0 * n));
            return t;
        }

        private static float[] BuildAlpha(int n)
        {
            var a = new float[n];
            double invN = 1.0 / n;
            a[0] = (float)Math.Sqrt(invN);
            for (int k = 1; k < n; k++) a[k] = (float)Math.Sqrt(2.0 * invN);
            return a;
        }

        private static float Median64(Span<float> values)
        {
            float[] buf = new float[values.Length];
            values.CopyTo(buf);
            Array.Sort(buf);
            return (buf[31] + buf[32]) * 0.5f;
        }
    }
}
