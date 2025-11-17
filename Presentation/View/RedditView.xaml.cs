using RedgifsDownloader.Presentation.ViewModel;
using System.Windows.Controls;

namespace RedgifsDownloader.View
{
    public partial class RedditView : UserControl
    {
        public RedditView()
        {
            InitializeComponent();
        }

        private void Logbox_TextChanged(object sender, EventArgs e)
        {
            LogBox.ScrollToEnd();
        }

        private async void UserControl_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            if (DataContext is RedditViewModel vm)
                await vm.CheckLoginStatusAsync();
        }
    }
}
