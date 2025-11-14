using RedgifsDownloader.Domain.Enums;
using RedgifsDownloader.Model;
using System.Globalization;
using System.Windows.Data;

namespace RedgifsDownloader.Converter
{
    public class DownloadStatusFilter : IValueConverter
    {
        public string Mode { get; set; } = "Active";

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var video = (VideoItem)value;
            return Mode switch
            {
                "Active" => video.Status != VideoStatus.Completed && video.Status != VideoStatus.Failed,
                "Failed" => video.Status is VideoStatus.WriteError or VideoStatus.NetworkError or VideoStatus.UnknownError or VideoStatus.Canceled,
                _ => true
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }
}
