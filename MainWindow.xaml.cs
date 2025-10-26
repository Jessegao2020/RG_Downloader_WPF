using RedgifsDownloader.Model;
using RedgifsDownloader.Services;
using RedgifsDownloader.ViewModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace RedgifsDownloader
{
    public partial class MainWindow : Window
    {
        #region ctor & Window lifecycle

        public MainWindow()
        {
            InitializeComponent();

            RestoreWindowPosition();
            
            DataContext = new MainViewModel();
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