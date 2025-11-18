using RedgifsDownloader.Domain.Interfaces;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System.Buffers;
using System.Diagnostics;
using System.Numerics;

namespace RedgifsDownloader.Infrastructure.ImageSim
{
    public class ImageHashService : IImageHashService
    {
        private const int N = 32;
        private const int K = 8;

        private static readonly float[,] Cos = BuildCos(N);
        private static readonly float[] Alpha = BuildAlpha(N);

        private static readonly ArrayPool<float> FloatPool = ArrayPool<float>.Shared;
        private static readonly ArrayPool<byte> BytePool = ArrayPool<byte>.Shared;

        public double Compare(ulong a, ulong b)
        {
            int diff = BitOperations.PopCount(a ^ b);
            return 1.0 - diff / 64.0;
        }

        public (ulong normal, ulong flipped, (double loadMs, double resizeMs, double dctMs)) ComputeHash(string path)
        {
            var sw = Stopwatch.StartNew();
            using var img = SixLabors.ImageSharp.Image.Load<Rgba32>(path);
            double loadMs = sw.Elapsed.TotalMilliseconds;

            sw.Restart();
            img.Mutate(x => x.Resize(N, N).Grayscale());
            double resizeMs = sw.Elapsed.TotalMilliseconds;

            byte[] gray = BytePool.Rent(N * N);
            try
            {
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
            finally
            {
                BytePool.Return(gray);
            }
        }

        private static ulong ComputeHashFromGray(byte[] gray)
        {
            int len = N * N;

            float[] input = FloatPool.Rent(len);
            float[] temp = FloatPool.Rent(len);
            float[] dct = FloatPool.Rent(len);

            try
            {
                for (int i = 0; i < len; i++)
                    input[i] = gray[i];

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

                // 取低频 8×8 块（跳过 DC）
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
                    if (ac[i] > median)
                        hash |= 1UL << i;

                return hash;
            }
            finally
            {
                FloatPool.Return(input);
                FloatPool.Return(temp);
                FloatPool.Return(dct);
            }
        }

        private static byte[] FlipGray(byte[] src, int side)
        {
            byte[] dst = BytePool.Rent(src.Length);
            try
            {
                for (int y = 0; y < side; y++)
                {
                    int baseIdx = y * side;
                    for (int x = 0; x < side; x++)
                        dst[baseIdx + x] = src[baseIdx + (side - 1 - x)];
                }

                return dst[..src.Length].ToArray();
            }
            finally
            {
                BytePool.Return(dst);
            }
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
