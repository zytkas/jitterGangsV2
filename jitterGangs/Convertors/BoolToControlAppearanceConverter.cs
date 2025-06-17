using System.Globalization;
using System.Windows.Data;
using Wpf.Ui.Controls;

namespace jitterGangs.Convertors
{
    public class BoolToControlAppearanceConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isRunning)
            {
                return isRunning ? ControlAppearance.Primary : ControlAppearance.Secondary;
            }
            return ControlAppearance.Secondary;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
