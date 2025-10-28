using RedgifsDownloader.Helpers;
using RedgifsDownloader.Interfaces;
using RedgifsDownloader.Services.Reddit;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;

namespace RedgifsDownloader.ViewModel
{

    public class RedditViewModel : INotifyPropertyChanged
    {
        private readonly IRedditAuthService _auth;
        private readonly IRedditApiService _api;

        private string _usernameInput = "";
        private string _submittedJson = "";
        private bool _isLoggedIn = false;

        public ICommand LoginCommand { get; }
        public ICommand GetSubmittedCommand { get; }

        public string UsernameInput
        {
            get => _usernameInput;
            set { _usernameInput = value; OnPropertyChanged(); }
        }

        public string SubmittedJson
        {
            get => _submittedJson;
            set { _submittedJson = value; OnPropertyChanged(); }
        }

        public bool IsLoggedIn
        {
            get => _isLoggedIn;
            set { _isLoggedIn = value; OnPropertyChanged(); }
        }

        public RedditViewModel(IRedditApiService api, IRedditAuthService auth)
        {
            _auth = auth;
            _api = api;

            LoginCommand = new RelayCommand(async _=> await  _auth.LoginAsync());
            GetSubmittedCommand = new RelayCommand(async _ => await GetSubmittedAsync(), _ => IsLoggedIn && !string.IsNullOrEmpty(UsernameInput));
        }

        private async Task GetSubmittedAsync()
        {
            try
            {
                SubmittedJson = await _api.GetUserSubmittedAsync(UsernameInput);
            }catch (Exception ex)
            {
                SubmittedJson = $"Error: {ex.Message}";
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string name = "") =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
