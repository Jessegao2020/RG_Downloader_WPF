using System.Buffers;
using System.Diagnostics;
using System.IO;
using System.Net.Http;

namespace RedgifsDownloader.Services
{
    public class DownloadWorker
    {
        private static readonly HttpClient _httpClient = new();

        public async Task<VideoStatus> DownloadAsync(string url, string path, string token, CancellationToken ct, Action<double>? onProgress = null)
        {
            if (ct.IsCancellationRequested || string.IsNullOrEmpty(url))
                return VideoStatus.Canceled;

            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Add("User-Agent", "Mozilla/5.0");
                request.Headers.Add("Referer", "https://www.redgifs.com/");
                request.Headers.Add("Authorization", "Bearer " + token);
                request.Headers.Add("Accept", "application/json, text/plain, */*");

                using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
                response.EnsureSuccessStatusCode();

                var totalBytes = response.Content.Headers.ContentLength ?? -1L;
                using var stream = await response.Content.ReadAsStreamAsync(ct);
                using var fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, 32768, useAsync: true);

                byte[] buffer = ArrayPool<byte>.Shared.Rent(32768);
                long totalRead = 0;

                try
                {
                    int read;
                    double lastPercent = 0;

                    while ((read = await stream.ReadAsync(buffer.AsMemory(0, buffer.Length), ct)) > 0)
                    {
                        await fs.WriteAsync(buffer.AsMemory(0, read), ct);
                        totalRead += read;

                        // CHECK: 此处暂时未重构进去
                        if (totalBytes > 0)
                        {
                            double percent = Math.Round((double)totalRead / totalBytes * 100, 1);
                            if (percent - lastPercent >= 1.0 || percent == 100.0)
                            {
                                lastPercent = percent;
                                onProgress?.Invoke(percent);
                            }
                        }
                    }
                    return VideoStatus.Completed;
                }
                catch (OperationCanceledException) { return VideoStatus.Canceled; }
                catch (IOException) { return VideoStatus.WriteError; }
                finally { ArrayPool<byte>.Shared.Return(buffer); }
            }
            catch (HttpRequestException) { return VideoStatus.NetworkError; }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                return VideoStatus.UnknownError;
            }
        }
    }
}
