using System;
using System.Collections.Generic;
using System.Linq;

namespace MenuStolovaya.Services
{
    /// <summary>
    /// Конвертер единиц измерения для продуктов (общепит)
    /// Все рецептуры ведутся в КИЛОГРАММАХ
    /// </summary>
    public static class UnitConverter
    {
        // Коэффициенты перевода в килограммы (для весовых единиц)
        private static readonly Dictionary<string, decimal> ConversionToKg = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase)
        {
            { "кг", 1m },      // 1 кг = 1 кг
            { "г", 0.001m },   // 1 г = 0.001 кг
        };

        /// <summary>
        /// Переводит количество продукта в килограммы
        /// </summary>
        /// <param name="quantity">Количество в исходных единицах</param>
        /// <param name="unit">Единица измерения (кг, г, л, мл, шт)</param>
        /// <param name="product">Продукт (для получения плотности или веса штуки)</param>
        /// <returns>Количество в килограммах</returns>
        public static decimal ConvertToKg(decimal quantity, string unit, Продукты product)
        {
            if (quantity <= 0) return 0;
            if (product == null) return quantity;

            unit = unit?.Trim().ToLower() ?? "кг";

            // Штуки - используем вес из БД
            if (unit == "шт")
            {
                decimal weightPerPiece = product.Вес_единицы_кг ?? 0.1m;
                if (weightPerPiece <= 0) weightPerPiece = 0.1m;
                return quantity * weightPerPiece;
            }

            // Объёмные единицы (л, мл) - используем плотность из БД
            if (unit == "л")
            {
                decimal density = product.Плотность_кг_л ?? 1.0m;
                return quantity * density;
            }

            if (unit == "мл")
            {
                decimal density = product.Плотность_кг_л ?? 1.0m;
                decimal liters = quantity / 1000m;
                return liters * density;
            }

            // Весовые единицы (кг, г)
            if (ConversionToKg.ContainsKey(unit))
            {
                return quantity * ConversionToKg[unit];
            }

            // По умолчанию считаем, что это кг
            return quantity;
        }

        /// <summary>
        /// Переводит килограммы обратно в исходные единицы для отображения
        /// </summary>
        public static string FormatQuantity(decimal quantityInKg, string targetUnit, Продукты product)
        {
            if (product == null) return $"{quantityInKg:F3} кг";

            targetUnit = targetUnit?.Trim().ToLower() ?? "кг";

            if (targetUnit == "шт")
            {
                decimal weightPerPiece = product.Вес_единицы_кг ?? 0.1m;
                if (weightPerPiece <= 0) weightPerPiece = 0.1m;
                decimal pieces = quantityInKg / weightPerPiece;
                return $"{pieces:F1} {targetUnit}";
            }

            if (targetUnit == "л")
            {
                decimal density = product.Плотность_кг_л ?? 1.0m;
                decimal liters = quantityInKg / density;
                return $"{liters:F3} {targetUnit}";
            }

            if (targetUnit == "мл")
            {
                decimal density = product.Плотность_кг_л ?? 1.0m;
                decimal liters = quantityInKg / density;
                decimal ml = liters * 1000;
                return $"{ml:F0} {targetUnit}";
            }

            if (targetUnit == "г")
            {
                decimal grams = quantityInKg * 1000;
                return $"{grams:F0} {targetUnit}";
            }

            return $"{quantityInKg:F3} {targetUnit}";
        }

        /// <summary>
        /// Получает цену за кг на основе цены за единицу и единицы измерения
        /// </summary>
        public static decimal GetPricePerKg(decimal pricePerUnit, string unit, Продукты product)
        {
            if (product == null) return pricePerUnit;

            unit = unit?.Trim().ToLower() ?? "кг";

            switch (unit)
            {
                case "кг":
                    return pricePerUnit;

                case "г":
                    return pricePerUnit * 1000;

                case "л":
                    decimal density = product.Плотность_кг_л ?? 1.0m;
                    return pricePerUnit / density;

                case "мл":
                    density = product.Плотность_кг_л ?? 1.0m;
                    return (pricePerUnit * 1000) / density;

                case "шт":
                    decimal weightPerPiece = product.Вес_единицы_кг ?? 0.1m;
                    if (weightPerPiece <= 0) weightPerPiece = 0.1m;
                    return pricePerUnit / weightPerPiece;

                default:
                    return pricePerUnit;
            }
        }

        /// <summary>
        /// Получает цену за единицу на основе цены за кг
        /// </summary>
        public static decimal GetPricePerUnit(decimal pricePerKg, string unit, Продукты product)
        {
            if (product == null) return pricePerKg;

            unit = unit?.Trim().ToLower() ?? "кг";

            switch (unit)
            {
                case "кг":
                    return pricePerKg;

                case "г":
                    return pricePerKg / 1000;

                case "л":
                    decimal density = product.Плотность_кг_л ?? 1.0m;
                    return pricePerKg * density;

                case "мл":
                    density = product.Плотность_кг_л ?? 1.0m;
                    return (pricePerKg * density) / 1000;

                case "шт":
                    decimal weightPerPiece = product.Вес_единицы_кг ?? 0.1m;
                    if (weightPerPiece <= 0) weightPerPiece = 0.1m;
                    return pricePerKg * weightPerPiece;

                default:
                    return pricePerKg;
            }
        }
    }
}