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
    }
}
