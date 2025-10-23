using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace RedgifsDownloader.Services
{
    public class DownloadService
    {
        private readonly HttpClient _http = new();

        public async Task DownloadAsync(
            VideoItem video,
            string baseDir,
            CancellationToken token,
            Action<double>? onProgress = null)
        {
            if (token.IsCancellationRequested || string.IsNullOrEmpty(video.Url)) return;

            string userName = string.IsNullOrWhiteSpace(video.Username) ? "Unknown" : video.Username.Trim();
            string saveDir = Path.Combine(baseDir, userName);
            Directory.CreateDirectory(saveDir);

            string path = Path.Combine(saveDir, video.Id + ".mp4");

            if (File.Exists(path))
            {
                video.Status = VideoStatus.Exists;
                video.Progress = 100;
                return;
            }

            using var request = new HttpRequestMessage(HttpMethod.Get, video.Url);
            request.Headers.Add("User-Agent", "Mozilla/5.0");
            request.Headers.Add("Referer", "https://www.redgifs.com/");
            request.Headers.Add("Authorization", "Bearer " + video.Token);
            request.Headers.Add("Accept", "application/json, text/plain, */*");

            using var response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, token);
            response.EnsureSuccessStatusCode();

            var totalBytes = response.Content.Headers.ContentLength ?? -1L;
            bool canReport = totalBytes > 0;

            using var stream = await response.Content.ReadAsStreamAsync(token);
            using var fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, 32768, useAsync: true);

            byte[] buffer = ArrayPool<byte>.Shared.Rent(32768);
            long totalRead = 0;
            try
            {
                int read;
                double lastPercent = 0;

                while ((read = await stream.ReadAsync(buffer.AsMemory(0, buffer.Length), token)) > 0)
                {
                    await fs.WriteAsync(buffer.AsMemory(0, read), token);
                    totalRead += read;

                    // CHECK: 此处暂时未重构进去
                    if (canReport)
                    {
                        double percent = Math.Round((double)totalRead / totalBytes * 100, 1);
                        if (percent - lastPercent >= 1.0 || percent == 100.0)
                        {
                            lastPercent = percent;
                            onProgress?.Invoke(percent);
                        }
                    }
                }

                video.Status = VideoStatus.Completed;
                video.Progress = 100;
            }
            catch (OperationCanceledException)
            {
                video.Status = VideoStatus.Canceled;
            }
            catch (IOException)
            {
                video.Status = VideoStatus.WriteError;
            }
            catch (HttpRequestException)
            {
                video.Status = VideoStatus.NetworkError;
            }
            catch
            {
                video.Status = VideoStatus.Failed;
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }
    }
}
