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
        private const int N = 32; // DCT matrix size
        private const int K = 8;  // Low-frequency block
        private static readonly float[,] Cos = BuildCos(N);
        private static readonly float[] Alpha = BuildAlpha(N);

        public static ulong ComputeHash(string path)
        {
            using var img = SixLabors.ImageSharp.Image.Load<Rgba32>(path);
            img.Mutate(x => x.Resize(N, N).Grayscale());

            // Extract grayscale bytes
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

            // Convert to float for DCT
            float[] input = new float[N * N];
            for (int i = 0; i < gray.Length; i++) input[i] = gray[i];

            // Perform 2D DCT (separable)
            float[] temp = new float[N * N];
            float[] dct = new float[N * N];

            // DCT rows
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

            // DCT columns
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

            // Extract 8x8 low-frequency AC coefficients (skip DC term)
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
            // Hamming similarity (0~1)
            int diff = BitOperations.PopCount(a ^ b);
            return 1.0 - diff / 64.0;
        }

        public static Dictionary<string, ulong> GetImageHashes(string folder, IProgress<(int done, int total)>? progress = null)
        {
            var dict = new ConcurrentDictionary<string, ulong>(StringComparer.OrdinalIgnoreCase);
            var exts = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg", ".png", ".bmp", ".webp", ".heic" };

            var files = Directory.EnumerateFiles(folder, "*.*", SearchOption.TopDirectoryOnly)
                                 .Where(f => exts.Contains(Path.GetExtension(f)))
                                 .ToList();

            int total = files.Count;
            int done = 0;

            Parallel.ForEach(files, new ParallelOptions { MaxDegreeOfParallelism = Math.Max(1, Environment.ProcessorCount - 2) }, file =>
            {
                try
                {
                    dict[file] = ComputeHash(file);
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
