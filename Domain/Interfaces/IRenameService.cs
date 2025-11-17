namespace RedgifsDownloader.Domain.Interfaces
{
    public interface IRenameService
    {
        Task<(int renamed, List<string> logs)> RenameAsync(string folderPath);
    }
}
