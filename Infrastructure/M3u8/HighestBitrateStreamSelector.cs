using System.Text.RegularExpressions;

namespace RedgifsDownloader.Infrastructure.M3u8
{
    /// <summary>
    /// 最高码率优先的流选择策略示例
    /// 适用于不限制编码格式，只要最高质量的场景
    /// </summary>
    public class HighestBitrateStreamSelector : IStreamSelector
    {
        private static readonly Regex VariantRegex = new(@"#EXT-X-STREAM-INF:([^\n]+)\n([^\n]+)", RegexOptions.Compiled);
        private static readonly Regex AudioRegex = new(@"#EXT-X-MEDIA:.*?TYPE=AUDIO.*?URI=""([^""]+)""", RegexOptions.Compiled);

        public Uri? SelectBestVideoStream(string masterM3u8, Uri baseUrl)
        {
            var variants = new List<(int bandwidth, string url)>();
            var matches = VariantRegex.Matches(masterM3u8);

            foreach (Match match in matches)
            {
                var streamInfo = match.Groups[1].Value;
                var variantUrl = match.Groups[2].Value.Trim();

                // 解析带宽 BANDWIDTH=6716095
                var bandwidthMatch = Regex.Match(streamInfo, @"BANDWIDTH=(\d+)");
                if (bandwidthMatch.Success && int.TryParse(bandwidthMatch.Groups[1].Value, out int bandwidth))
                {
                    var fullUrl = new Uri(baseUrl, variantUrl).ToString();
                    variants.Add((bandwidth, fullUrl));
                }
            }

            // 选择带宽最高的流
            var best = variants.OrderByDescending(v => v.bandwidth).FirstOrDefault();
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


