namespace RedgifsDownloader.ApplicationLayer.ImageSimilarity
{
    public interface IImageSimilarityAppService
    {
        Dictionary<string, (ulong normal, ulong flipped)> GetImageHashes(
                                                                string folder,
                                                                IProgress<(int done, int total)>? progress = null,
                                                                Action<string>? log = null);

        Dictionary<int, List<string>> GroupSimilarImages(Dictionary<string, (ulong normal, ulong flipped)> hashes, double threshold);
    }
}
