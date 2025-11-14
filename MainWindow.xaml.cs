using Microsoft.Extensions.DependencyInjection;
using RedgifsDownloader.Presentation.ViewModel;
using System.ComponentModel;
using System.Windows;

namespace RedgifsDownloader
{
    public partial class MainWindow : Window
    {
        #region ctor & Window lifecycle
        public MainWindow()
        {
            InitializeComponent();
            RestoreWindowPosition();
            
            DataContext = App.ServiceProvider.GetRequiredService<MainViewModel>();
        }

        private void RestoreWindowPosition()
        {
            this.Top = Properties.Settings.Default.WindowTop;
            this.Left = Properties.Settings.Default.WindowLeft;
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {

            SaveWindowPositionToSettings();
            Properties.Settings.Default.Save();
        }

        private void SaveWindowPositionToSettings()
        {
            if (this.WindowState == WindowState.Normal)
            {
                Properties.Settings.Default.WindowTop = this.Top;
                Properties.Settings.Default.WindowLeft = this.Left;
            }
        }
        #endregion
    }
}