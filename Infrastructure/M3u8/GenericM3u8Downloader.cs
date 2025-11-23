using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text.RegularExpressions;
using RedgifsDownloader.ApplicationLayer.DTOs;
using RedgifsDownloader.Domain.Enums;
using RedgifsDownloader.Domain.Interfaces;

namespace RedgifsDownloader.Infrastructure.M3u8
{
    public class GenericM3u8Downloader : ITransferDownloader
    {
        private readonly HttpClient _httpClient;
        private readonly IStreamSelector _streamSelector;
        private static readonly Regex SegmentRegex = new(@"^(?!#)(.+\.(?:ts|m4s|mp4|m4a))", RegexOptions.Compiled | RegexOptions.Multiline);
        private static readonly Regex InitSegmentRegex = new(@"#EXT-X-MAP:URI=""([^""]+)""", RegexOptions.Compiled);

        public GenericM3u8Downloader(HttpClient httpClient, IStreamSelector streamSelector)
        {
            _httpClient = httpClient;
            _streamSelector = streamSelector;
        }

        public async Task<DownloadResult> DownloadAsync(
            Uri url,
            string outputPath,
            MediaDownloadContext context,
            CancellationToken ct = default,
            IProgress<double>? progress = null)
        {
            string tempPath = outputPath + ".mp4.tmp";

            try
            {
                var masterM3u8 = await DownloadTextAsync(url, context, ct);

                var bestVariantUrl = _streamSelector.SelectBestVideoStream(masterM3u8, url);
                if (bestVariantUrl == null)
                    return new DownloadResult(VideoStatus.UnknownError, 0);

                var audioUrl = _streamSelector.ExtractAudioStream(masterM3u8, url);

                var videoM3u8 = await DownloadTextAsync(bestVariantUrl, context, ct);

                var (videoInit, videoSegments) = ExtractSegments(videoM3u8, bestVariantUrl);
                if (videoSegments.Count == 0)
                    return new DownloadResult(VideoStatus.UnknownError, 0);

                // 如果有音频流，下载音频流 M3U8
                Uri? audioInit = null;
                List<Uri> audioSegments = new();
                if (audioUrl != null)
                {
                    var audioM3u8 = await DownloadTextAsync(audioUrl, context, ct);
                    (audioInit, audioSegments) = ExtractSegments(audioM3u8, audioUrl);
                }

                // 下载音视频到临时文件
                string videoTempPath = outputPath + "_video.tmp";
                string audioTempPath = outputPath + "_audio.tmp";
                string finalPath = outputPath + ".mp4";

                try
                {
                    // 下载视频流（0-50%）
                    await DownloadStreamAsync(videoInit, videoSegments, videoTempPath, context, progress, ct, 0, 50);

                    if (audioSegments.Count > 0)
                    {
                        await DownloadStreamAsync(audioInit, audioSegments, audioTempPath, context, progress, ct, 50, 100);

                        await MergeAudioVideoAsync(videoTempPath, audioTempPath, finalPath, ct);
                    }
                    else
                    {
                        // 没有音频，直接重命名视频文件
                        if (File.Exists(finalPath))
                            File.Delete(finalPath);
                        File.Move(videoTempPath, finalPath);
                    }

                    progress?.Report(100);

                    return new DownloadResult(VideoStatus.Completed, new FileInfo(finalPath).Length);
                }
                finally
                {
                    DeleteFile(videoTempPath);
                    DeleteFile(audioTempPath);
                }
            }
            catch (OperationCanceledException)
            {
                DeleteFile(tempPath);
                return new DownloadResult(VideoStatus.Canceled, 0);
            }
            catch (HttpRequestException)
            {
                DeleteFile(tempPath);
                return new DownloadResult(VideoStatus.NetworkError, 0);
            }
            catch
            {
                DeleteFile(tempPath);
                return new DownloadResult(VideoStatus.UnknownError, 0);
            }
        }

        private async Task<string> DownloadTextAsync(Uri url, MediaDownloadContext context, CancellationToken ct)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            AddHeaders(request, context);

            // 显式设置 Accept-Encoding 为 identity，避免服务器返回压缩内容
            request.Headers.Remove("Accept-Encoding");
            request.Headers.TryAddWithoutValidation("Accept-Encoding", "identity");

            var response = await _httpClient.SendAsync(request, ct);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsStringAsync(ct);
        }

        private (Uri? initSegment, List<Uri> segments) ExtractSegments(string variantM3u8, Uri baseUrl)
        {
            // 提取初始化分段
            Uri? initSegment = null;
            var initMatch = InitSegmentRegex.Match(variantM3u8);
            if (initMatch.Success)
            {
                var initPath = initMatch.Groups[1].Value;
                initSegment = new Uri(baseUrl, initPath);
            }

            // 提取所有媒体分段
            var segments = new List<Uri>();
            foreach (Match match in SegmentRegex.Matches(variantM3u8))
            {
                var segmentPath = match.Groups[1].Value.Trim();
                var segmentUrl = new Uri(baseUrl, segmentPath);
                segments.Add(segmentUrl);
            }

            return (initSegment, segments);
        }

        private async Task DownloadStreamAsync(
            Uri? initSegment,
            List<Uri> segments,
            string outputPath,
            MediaDownloadContext context,
            IProgress<double>? progress,
            CancellationToken ct,
            int progressStart = 0,
            int progressEnd = 100)
        {
            using var fileStream = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.None);

            // 下载初始化分段
            if (initSegment != null)
            {
                await DownloadSegmentAsync(initSegment, fileStream, context, ct);
            }

            // 下载媒体分段
            for (int i = 0; i < segments.Count; i++)
            {
                ct.ThrowIfCancellationRequested();
                await DownloadSegmentAsync(segments[i], fileStream, context, ct);

                // 计算当前进度
                double segmentProgress = (double)(i + 1) / segments.Count;
                double currentProgress = progressStart + (progressEnd - progressStart) * segmentProgress;
                progress?.Report(currentProgress);
            }
        }

        private async Task MergeAudioVideoAsync(string videoPath, string audioPath, string outputPath, CancellationToken ct)
        {
            if (File.Exists(outputPath))
                File.Delete(outputPath);

            string ffmpegPath = FindFfmpegPath();
            if (string.IsNullOrEmpty(ffmpegPath))
            {
                throw new FileNotFoundException(
                    "找不到 ffmpeg.exe。请下载 ffmpeg 并放到程序 bin 目录或添加到系统 PATH。\n" +
                    "下载地址：https://github.com/BtbN/FFmpeg-Builds/releases");
            }

            string arguments = $"-i \"{videoPath}\" -i \"{audioPath}\" -c copy -movflags +faststart -y \"{outputPath}\"";

            var startInfo = new ProcessStartInfo
            {
                FileName = ffmpegPath,
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = startInfo };
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            await process.WaitForExitAsync(ct);

            if (process.ExitCode != 0)
                throw new Exception($"FFmpeg 合并失败，退出码: {process.ExitCode}");
        }

        private string FindFfmpegPath()
        {
            // 1. 检查程序 bin 目录
            string binPath = Path.Combine(AppContext.BaseDirectory, "ffmpeg.exe");
            if (File.Exists(binPath))
                return binPath;

            // 2. 检查 bin 子文件夹
            string binSubPath = Path.Combine(AppContext.BaseDirectory, "bin", "ffmpeg.exe");
            if (File.Exists(binSubPath))
                return binSubPath;

            // 3. 检查系统 PATH
            string? pathEnv = Environment.GetEnvironmentVariable("PATH");
            if (pathEnv != null)
            {
                foreach (var path in pathEnv.Split(Path.PathSeparator))
                {
                    string fullPath = Path.Combine(path, "ffmpeg.exe");
                    if (File.Exists(fullPath))
                        return fullPath;
                }
            }

            return string.Empty;
        }

        private async Task<long> DownloadSegmentAsync(Uri segmentUrl, FileStream fileStream, MediaDownloadContext context, CancellationToken ct)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, segmentUrl);
            AddHeaders(request, context);

            var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
            response.EnsureSuccessStatusCode();

            using var stream = await response.Content.ReadAsStreamAsync(ct);
            long startPos = fileStream.Position;
            await stream.CopyToAsync(fileStream, ct);
            return fileStream.Position - startPos;
        }

        private void AddHeaders(HttpRequestMessage request, MediaDownloadContext context)
        {
            if (context?.Headers == null) return;

            foreach (var (key, value) in context.Headers)
                request.Headers.TryAddWithoutValidation(key, value);
        }

        private void DeleteFile(string path)
        {
            try { if (File.Exists(path)) File.Delete(path); }
            catch { }
        }
    }
}


