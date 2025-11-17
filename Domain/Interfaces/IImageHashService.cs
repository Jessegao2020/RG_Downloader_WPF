namespace RedgifsDownloader.Domain.Interfaces
{
    public interface IImageHashService
    {
        (ulong normal, ulong flipped, (double loadMs, double resizeMs, double dctMs)) ComputeHash(string path);

        double Compare(ulong a, ulong b);
    }
}
