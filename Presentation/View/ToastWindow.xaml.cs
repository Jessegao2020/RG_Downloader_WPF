using System.Windows;
using System.Windows.Threading;

namespace RedgifsDownloader.View
{
    public partial class ToastWindow : Window
    {
        public ToastWindow(string message)
        {
            InitializeComponent();
            TextBlockMessage.Text = message;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1.5) };
            timer.Tick += (s, _) =>
            {
                timer.Stop();
                Close();
            };
            timer.Start();
        }
    }
}
