using System.Windows.Data;
using System.Globalization;

namespace RedgifsDownloader
{
    public class ColumnWidthConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double totalWidth && parameter != null)
            {
                if (double.TryParse(parameter.ToString(), out double ratio))
                    return totalWidth * ratio;
            }
            return 100.0; // 默认宽度
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
