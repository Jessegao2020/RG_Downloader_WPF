using System.ComponentModel;
using System.Windows.Input;
using RedgifsDownloader.ApplicationLayer.Reddit;
using RedgifsDownloader.Helpers;

namespace RedgifsDownloader.Presentation.ViewModel
{
    public class RedditViewModelNew : INotifyPropertyChanged
    {
        private readonly IRedditDownloadAppService _redditApp;

        public string Username { get; set; } = "";
        public bool IsVideoMode { get; set; }

        public string Log { get; set; } = "";
        public int Progress { get; set; }

        public ICommand LoginCommand { get; }
        public ICommand DownloadCommand { get; }

        public RedditViewModelNew(IRedditDownloadAppService redditApp)
        {
            _redditApp = redditApp;

            LoginCommand = new RelayCommand(async _ => await Login());
            DownloadCommand = new RelayCommand(async _ => await Download());
        }

        private async Task Login()
        {
            bool ok = await _redditApp.LoginAsync();
            Log += ok ? "登陆成功\n" : "登陆失败\n";
        }

        private async Task Download()
        {
            Log += "开始下载...\n";

            var summary = await _redditApp.DownloadUserAsync(Username, IsVideoMode, msg => Log += msg + "\n", p => Progress = p);

            Log += $"完成： 成功={summary.Success}，失败={summary.Fail}\n";
        }

        public event PropertyChangedEventHandler? PropertyChanged;
    }
}
