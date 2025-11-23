using System.Text.RegularExpressions;

namespace RedgifsDownloader.Infrastructure.M3u8
{
    /// <summary>
    /// AVC (H.264) 优先的流选择策略
    /// 优先选择 AVC1 编码，排除 VP9 和 HEVC，按分辨率降序排序
    /// </summary>
    public class AvcPreferredStreamSelector : IStreamSelector
    {
        private static readonly Regex VariantRegex = new(@"#EXT-X-STREAM-INF:([^\n]+)\n([^\n]+)", RegexOptions.Compiled);
        private static readonly Regex AudioRegex = new(@"#EXT-X-MEDIA:.*?TYPE=AUDIO.*?URI=""([^""]+)""", RegexOptions.Compiled);

        public Uri? SelectBestVideoStream(string masterM3u8, Uri baseUrl)
        {
            var variants = new List<(int width, string url, bool isAvc1)>();
            var matches = VariantRegex.Matches(masterM3u8);

            foreach (Match match in matches)
            {
                var streamInfo = match.Groups[1].Value;
                var variantUrl = match.Groups[2].Value.Trim();

                // 排除 VP9 和 HEVC 编码，只要 AVC1 (H.264)
                bool isAvc1 = streamInfo.Contains("avc1");
                bool isVp9 = streamInfo.Contains("vp09") || streamInfo.Contains("vp9");
                bool isHevc = streamInfo.Contains("hvc1") || streamInfo.Contains("hevc");

                if (isVp9 || isHevc)
                    continue;

                // 解析分辨率
                var resolutionMatch = Regex.Match(streamInfo, @"RESOLUTION=(\d+)x(\d+)");
                if (resolutionMatch.Success && int.TryParse(resolutionMatch.Groups[1].Value, out int width))
                {
                    var fullUrl = new Uri(baseUrl, variantUrl).ToString();
                    variants.Add((width, fullUrl, isAvc1));
                }
            }

            // 优先选择 AVC1，然后按分辨率降序排序
            var best = variants
                .OrderByDescending(v => v.isAvc1)
                .ThenByDescending(v => v.width)
                .FirstOrDefault();

            return best.url != null ? new Uri(best.url) : null;
        }

        public Uri? ExtractAudioStream(string masterM3u8, Uri baseUrl)
        {
            var match = AudioRegex.Match(masterM3u8);
            if (match.Success)
            {
                var audioPath = match.Groups[1].Value;
                return new Uri(baseUrl, audioPath);
            }
            return null;
        }
    }
}


