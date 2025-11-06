using System.Globalization;
using System.Windows.Data;

namespace RedgifsDownloader.Converter
{
    public class InverseBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool b)
                return !b;

            // 传入 null 或非 bool 时，WPF 不做任何写入操作
            return Binding.DoNothing;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool b)
                return !b;

            return Binding.DoNothing;
        }
    }
}
