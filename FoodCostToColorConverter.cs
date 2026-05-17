using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media;

namespace MenuStolovaya
{
    public class FoodCostToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string foodCostStr && decimal.TryParse(foodCostStr.Replace("%", ""), out decimal foodCost))
            {
                if (foodCost <= 25)
                    return Brushes.Green;
                else if (foodCost <= 35)
                    return Brushes.Orange;
                else
                    return Brushes.Red;
            }
            return Brushes.Black;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
