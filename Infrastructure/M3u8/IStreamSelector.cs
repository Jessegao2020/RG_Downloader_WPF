namespace RedgifsDownloader.Infrastructure.M3u8
{
    /// <summary>
    /// M3U8 流选择策略接口
    /// </summary>
    public interface IStreamSelector
    {
        /// <summary>
        /// 从主 M3U8 中选择最佳视频流
        /// </summary>
        /// <param name="masterM3u8">主 M3U8 内容</param>
        /// <param name="baseUrl">基础 URL</param>
        /// <returns>最佳视频流 URL，如果未找到返回 null</returns>
        Uri? SelectBestVideoStream(string masterM3u8, Uri baseUrl);

        /// <summary>
        /// 从主 M3U8 中提取音频流 URL
        /// </summary>
        /// <param name="masterM3u8">主 M3U8 内容</param>
        /// <param name="baseUrl">基础 URL</param>
        /// <returns>音频流 URL，如果未找到返回 null</returns>
        Uri? ExtractAudioStream(string masterM3u8, Uri baseUrl);
    }
}

