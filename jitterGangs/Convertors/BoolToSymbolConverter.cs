using System.Globalization;
using System.Windows.Data;
using Wpf.Ui.Controls;

namespace jitterGangs.Convertors
{
    public class BoolToSymbolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isReady)
            {
                return isReady ? SymbolRegular.Checkmark16 : SymbolRegular.ErrorCircle16;
            }
            return SymbolRegular.ErrorCircle16;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
