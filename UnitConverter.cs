using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System;
using System.Collections.Generic;

namespace MenuStolovaya.Services
{
    /// <summary>
    /// Конвертер единиц измерения для продуктов (общепит)
    /// Все рецептуры ведутся в КИЛОГРАММАХ
    /// </summary>
    public static class UnitConverter
    {
        // Коэффициенты перевода в килограммы
        private static readonly Dictionary<string, decimal> ConversionToKg = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase)
        {
            { "кг", 1m },      // 1 кг = 1 кг
            { "г", 0.001m },   // 1 г = 0.001 кг
            { "л", 1m },       // 1 л воды ≈ 1 кг (для других жидкостей нужна плотность)
            { "мл", 0.001m },  // 1 мл = 0.001 кг (для воды)
            { "шт", 0m }       // Штуки - требуют среднего веса
        };

        // Плотность продуктов (кг/л) для перевода объёма в массу
        private static readonly Dictionary<string, decimal> Density = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase)
        {
            { "молоко", 1.03m },      // 1 л молока ≈ 1.03 кг
            { "сливки", 1.01m },      // 1 л сливок ≈ 1.01 кг
            { "масло растительное", 0.92m }, // 1 л масла ≈ 0.92 кг
            { "масло подсолнечное", 0.92m },
            { "вода", 1.0m },
            { "бульон", 1.02m },
            { "сок", 1.05m },
            { "мёд", 1.42m },         // 1 л мёда ≈ 1.42 кг
            { "кефир", 1.03m },
            { "йогурт", 1.04m },
            { "сметана", 1.02m }
        };

        /// <summary>
        /// Переводит количество продукта в килограммы
        /// </summary>
        /// <param name="quantity">Количество</param>
        /// <param name="unit">Единица измерения (кг, г, л, мл, шт)</param>
        /// <param name="productName">Название продукта (для определения плотности)</param>
        /// <returns>Количество в килограммах</returns>
        public static decimal ConvertToKg(decimal quantity, string unit, string productName = "")
        {
            if (quantity <= 0) return 0;

            unit = unit?.Trim().ToLower() ?? "кг";

            // Штуки - нужен средний вес
            if (unit == "шт")
            {
                return ConvertPiecesToKg(quantity, productName);
            }

            // Объёмные единицы (л, мл)
            if (unit == "л" || unit == "мл")
            {
                decimal liters = unit == "л" ? quantity : quantity / 1000m;
                decimal density = GetDensity(productName);
                return liters * density;
            }

            // Весовые единицы
            if (ConversionToKg.ContainsKey(unit))
            {
                return quantity * ConversionToKg[unit];
            }

            // По умолчанию считаем, что это кг
            return quantity;
        }

        /// <summary>
        /// Переводит штуки в килограммы (средний вес продукта)
        /// </summary>
        private static decimal ConvertPiecesToKg(decimal pieces, string productName)
        {
            // Средний вес популярных продуктов (в кг)
            var averageWeight = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase)
            {
                { "яйцо", 0.05m },       // 1 яйцо ≈ 50 г
                { "яйца", 0.05m },
                { "лук", 0.08m },         // 1 луковица ≈ 80 г
                { "лук репчатый", 0.08m },
                { "картофель", 0.12m },   // 1 картофелина ≈ 120 г
                { "морковь", 0.10m },     // 1 морковь ≈ 100 г
                { "помидор", 0.10m },     // 1 помидор ≈ 100 г
                { "огурец", 0.12m },      // 1 огурец ≈ 120 г
                { "перец", 0.15m },       // 1 перец ≈ 150 г
                { "чеснок", 0.005m },     // 1 зубчик ≈ 5 г
                { "лимон", 0.10m },       // 1 лимон ≈ 100 г
                { "апельсин", 0.15m },    // 1 апельсин ≈ 150 г
                { "банан", 0.15m },       // 1 банан ≈ 150 г
                { "хлеб", 0.03m },        // 1 кусок хлеба ≈ 30 г (для бутербродов)
                { "булка", 0.30m },       // 1 булка ≈ 300 г
            };

            // Ищем средний вес по названию продукта
            foreach (var kvp in averageWeight)
            {
                if (productName.ToLower().Contains(kvp.Key))
                {
                    return pieces * kvp.Value;
                }
            }

            // Если продукт не найден, показываем предупреждение
            System.Diagnostics.Debug.WriteLine($"ВНИМАНИЕ: Не указан средний вес для продукта '{productName}' в штуках!");
            return pieces * 0.1m; // По умолчанию 100 г
        }

        /// <summary>
        /// Получает плотность продукта (кг/л)
        /// </summary>
        private static decimal GetDensity(string productName)
        {
            if (string.IsNullOrEmpty(productName)) return 1.0m;

            foreach (var kvp in Density)
            {
                if (productName.ToLower().Contains(kvp.Key))
                {
                    return kvp.Value;
                }
            }

            // По умолчанию - плотность воды
            return 1.0m;
        }

        /// <summary>
        /// Получает коэффициент перевода для единицы измерения
        /// </summary>
        public static decimal GetConversionFactor(string unit)
        {
            if (ConversionToKg.ContainsKey(unit))
                return ConversionToKg[unit];
            return 1m;
        }

        /// <summary>
        /// Форматирует количество для отображения с правильной единицей
        /// </summary>
        public static string FormatQuantity(decimal quantityInKg, string originalUnit, string productName = "")
        {
            if (originalUnit == "шт")
            {
                var pieces = ConvertKgToPieces(quantityInKg, productName);
                return $"{pieces:F1} {originalUnit}";
            }

            if (originalUnit == "л" || originalUnit == "мл")
            {
                decimal liters = quantityInKg / GetDensity(productName);
                if (originalUnit == "мл")
                    return $"{liters * 1000:F0} {originalUnit}";
                return $"{liters:F3} {originalUnit}";
            }

            if (originalUnit == "г")
                return $"{quantityInKg * 1000:F0} {originalUnit}";

            return $"{quantityInKg:F3} {originalUnit}";
        }

        /// <summary>
        /// Переводит килограммы обратно в штуки
        /// </summary>
        private static decimal ConvertKgToPieces(decimal kg, string productName)
        {
            var averageWeight = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase)
            {
                { "яйцо", 0.05m },
                { "яйца", 0.05m },
                { "лук", 0.08m },
                { "картофель", 0.12m },
                { "морковь", 0.10m },
                { "помидор", 0.10m },
                { "огурец", 0.12m }
            };

            foreach (var kvp in averageWeight)
            {
                if (productName.ToLower().Contains(kvp.Key))
                {
                    return kg / kvp.Value;
                }
            }

            return kg / 0.1m;
        }
    }
}