using RedgifsDownloader.Domain.Interfaces;

namespace RedgifsDownloader.ApplicationLayer.DupeCleaner
{
    public class DupeCleanerAppService
    {
        private readonly IDupeCleanerService _cleaner;
        private readonly IRenameService _renamer;

        public DupeCleanerAppService(IDupeCleanerService cleaner, IRenameService renamer)
        {
            _cleaner = cleaner;
            _renamer = renamer;
        }

        public Task<(int kept, int deleted, List<string> logs)> CleanAsync(string folder)
            => _cleaner.CleanAsync(folder);

        public Task<(int renamed, List<string> logs)> RenameAsync(string folder)
            => _renamer.RenameAsync(folder);
    }
}
