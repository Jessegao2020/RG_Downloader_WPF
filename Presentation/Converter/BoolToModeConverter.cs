using System.Globalization;
using System.Windows.Data;

namespace RedgifsDownloader.Presentation.Converter
{
    public class BoolToModeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isAdvanced)
                return isAdvanced ? "高级版" : "简单版";
            return "高级版";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string str)
                return str == "高级版";
            return true;
        }
    }
}

