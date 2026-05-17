using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Globalization;
using System.Windows.Data;

namespace MenuStolovaya.Views
{
    public class DecimalConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is decimal decimalValue)
            {
                return decimalValue.ToString(CultureInfo.InvariantCulture);
            }
            else if (value is double doubleValue)
            {
                return doubleValue.ToString(CultureInfo.InvariantCulture);
            }
            else if (value is float floatValue)
            {
                return floatValue.ToString(CultureInfo.InvariantCulture);
            }
            else if (value is int intValue)
            {
                return intValue.ToString(CultureInfo.InvariantCulture);
            }

            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string stringValue = value as string;
            if (string.IsNullOrWhiteSpace(stringValue))
            {
                if (targetType == typeof(decimal?))
                    return null;
                else if (targetType == typeof(double?))
                    return null;
                else if (targetType == typeof(float?))
                    return null;

                return 0;
            }

            // Заменяем запятую на точку для корректного парсинга
            stringValue = stringValue.Replace(',', '.');

            try
            {
                if (targetType == typeof(decimal) || targetType == typeof(decimal?))
                {
                    return decimal.Parse(stringValue, CultureInfo.InvariantCulture);
                }
                else if (targetType == typeof(double) || targetType == typeof(double?))
                {
                    return double.Parse(stringValue, CultureInfo.InvariantCulture);
                }
                else if (targetType == typeof(float) || targetType == typeof(float?))
                {
                    return float.Parse(stringValue, CultureInfo.InvariantCulture);
                }
                else if (targetType == typeof(int) || targetType == typeof(int?))
                {
                    return int.Parse(stringValue, CultureInfo.InvariantCulture);
                }
            }
            catch
            {
                // В случае ошибки парсинга возвращаем 0
            }

            if (targetType == typeof(decimal?))
                return null;
            else if (targetType == typeof(double?))
                return null;
            else if (targetType == typeof(float?))
                return null;

            return 0;
        }
    }
}
