using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace jitterGangs.Convertors
{
    public class BoolToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isReady)
            {
                return isReady ? new SolidColorBrush(Colors.LightGreen) : new SolidColorBrush(Colors.Orange);
            }
            return new SolidColorBrush(Colors.Red);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
