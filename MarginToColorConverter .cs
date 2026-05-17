using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace MenuStolovaya.Views
{
    public class MarginToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string margin)
            {
                if (margin == "Высокомаржинальное")
                    return Brushes.Green;
                else if (margin == "Среднемаржинальное")
                    return Brushes.Orange;
                else if (margin == "Низкомаржинальное")
                    return Brushes.Red;
                else
                    return Brushes.Black;
            }
            return Brushes.Black;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}