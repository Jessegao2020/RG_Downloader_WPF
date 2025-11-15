namespace RedgifsDownloader.Domain.Interfaces
{
    public interface IFileStorage
    {
        string GetBaseDirectory();
        string CombinePath(params string[] segments);
        bool FileExists(string path);   
        long GetFileSize(string path);
        void CreateDirectory(string path);
    }
}
