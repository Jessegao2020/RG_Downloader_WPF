namespace RedgifsDownloader.Domain.Interfaces
{
    public interface IDupeFileMoveService
    {
        void MoveToDupeFolder(string baseFolder, Dictionary<int, List<string>> groups);
    }
}
