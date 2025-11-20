using System.Diagnostics;
using System.Text.RegularExpressions;
using RedgifsDownloader.ApplicationLayer.DTOs;
using RedgifsDownloader.Domain.Enums;
using RedgifsDownloader.Domain.Interfaces;

namespace RedgifsDownloader.Infrastructure.Fikfap
{
    public class YtDlpTransferDownloader : ITransferDownloader
    {
        private readonly string _ytDlpPath;

        public YtDlpTransferDownloader(string ytDlpPath = "yt-dlp.exe")
        {
            _ytDlpPath = ytDlpPath;
        }
        public async Task<DownloadResult> DownloadAsync(Uri url, string outputPath, MediaDownloadContext context, CancellationToken ct = default, IProgress<double>? progress = null)
        {
            var psi = new ProcessStartInfo
            {
                FileName = _ytDlpPath,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            psi.ArgumentList.Add("-S");
            psi.ArgumentList.Add("vcodec:avc1,res");
            psi.ArgumentList.Add("-f");
            psi.ArgumentList.Add("bv+ba");
            psi.ArgumentList.Add("-o");
            psi.ArgumentList.Add(outputPath);
            psi.ArgumentList.Add(url.ToString());

            AddHeadersToArgs(context, ref psi);

            using var process = new Process { StartInfo = psi, EnableRaisingEvents = true };

            var tcs = new TaskCompletionSource<bool>();

            process.OutputDataReceived += (s, e) =>
            {
                if (e.Data != null)
                    TryParseProgress(e.Data, progress);
            };

            process.OutputDataReceived += (s, e) =>
            {

            };

            ct.Register(() =>
            {
                try { if (!process.HasExited) process.Kill(); } catch { }
            });

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            try
            {
                await process.WaitForExitAsync(ct);
            }
            catch (TaskCanceledException)
            {
                return new DownloadResult(VideoStatus.Canceled, 0);
            }

            if (ct.IsCancellationRequested)
                return new DownloadResult(VideoStatus.Canceled, 0);

            if (process.ExitCode == 0)
                return new DownloadResult(VideoStatus.Completed, 0);

            return new DownloadResult(VideoStatus.UnknownError, 0);
        }

        private void TryParseProgress(string line, IProgress<double>? progress)
        {
            if (progress == null) return;

            var match = Regex.Match(line, @"(\d+\.\d+)%");

            if (match.Success && double.TryParse(match.Groups[1].Value, out double percent))
            {
                progress.Report(percent);
            }
        }

        private void AddHeadersToArgs(MediaDownloadContext context, ref ProcessStartInfo psi)
        {
            if (context?.Headers == null)
                return;

            foreach (var (key, value) in context.Headers)
            {
                psi.ArgumentList.Add("--add-header");
                psi.ArgumentList.Add($"{key}:{value}");
            }
        }
    }
}

