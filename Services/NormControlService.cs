using MenuStolovaya.Views;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MenuStolovaya.Services
{
    /// <summary>
    /// Сервис нормоконтроля пищевой ценности
    /// Основан на:
    /// - МР 2.3.1.0253-21 (Нормы физиологических потребностей)
    /// - СанПиН 2.3/2.4.3590-20
    /// - ГОСТ 31987-2012
    /// - Рекомендации для III группы физической активности (работники промышленных предприятий)
    /// </summary>
    public static class NormControlService
    {
        // Рекомендуемые нормы для III группы физической активности (работники фабрики)
        private static readonly DailyNorm DefaultNorm = new DailyNorm
        {
            Calories = 3000,  // ккал/сутки (средняя тяжесть труда)
            Protein = 100,    // г/сутки (13.5% от калорийности)
            Fat = 100,        // г/сутки (30% от калорийности)
            Carbs = 420       // г/сутки (56.5% от калорийности)
        };

        // Распределение по приёмам пищи (рекомендовано Роспотребнадзором)
        private static readonly Dictionary<string, MealDistribution> MealDistribution = new Dictionary<string, MealDistribution>
        {
            { "Завтрак", new MealDistribution { Percentage = 25, Calories = 750, Protein = 25, Fat = 25, Carbs = 105 } },
            { "Обед", new MealDistribution { Percentage = 35, Calories = 1050, Protein = 35, Fat = 35, Carbs = 147 } },
            { "Ужин", new MealDistribution { Percentage = 25, Calories = 750, Protein = 25, Fat = 25, Carbs = 105 } },
            { "Полдник", new MealDistribution { Percentage = 15, Calories = 450, Protein = 15, Fat = 15, Carbs = 63 } }
        };

        // Допустимые отклонения (%)
        private static readonly Deviation Tolerance = new Deviation
        {
            Calories = 15,   // ±15%
            Protein = 20,    // ±20%
            Fat = 20,        // ±20%
            Carbs = 20       // ±20%
        };

        public static NormCheckResult CheckMenu(List<MenuItemWithDetails> menuItems)
        {
            if (menuItems == null || !menuItems.Any())
                return new NormCheckResult { IsValid = true, Warnings = new List<NormWarning>() };

            var result = new NormCheckResult
            {
                IsValid = true,
                Warnings = new List<NormWarning>(),
                Totals = new NutrientTotal(),
                RecommendedTotals = new NutrientTotal()
            };

            // Устанавливаем рекомендуемые значения
            result.RecommendedTotals.Calories = DefaultNorm.Calories;
            result.RecommendedTotals.Protein = DefaultNorm.Protein;
            result.RecommendedTotals.Fat = DefaultNorm.Fat;
            result.RecommendedTotals.Carbs = DefaultNorm.Carbs;

            // Группируем по времени подачи
            var groups = menuItems.GroupBy(m => m.Время_подачи);

            foreach (var group in groups)
            {
                string mealTime = group.Key;

                MealDistribution mealNorm = null;
                if (MealDistribution.ContainsKey(mealTime))
                    mealNorm = MealDistribution[mealTime];
                else
                    continue;

                decimal totalCalories = group.Sum(m => m.Калорийность_на_порцию);
                decimal totalProtein = group.Sum(m => CalculateProtein(m));
                decimal totalFat = group.Sum(m => CalculateFat(m));
                decimal totalCarbs = group.Sum(m => CalculateCarbs(m));

                // Добавляем к общим итогам
                result.Totals.Calories += totalCalories;
                result.Totals.Protein += totalProtein;
                result.Totals.Fat += totalFat;
                result.Totals.Carbs += totalCarbs;

                // Проверяем каждый приём пищи
                CheckMeal(mealTime, totalCalories, totalProtein, totalFat, totalCarbs, mealNorm, result);
            }

            // Проверяем суточные итоги
            CheckDailyTotals(result);

            return result;
        }

        private static void CheckMeal(string mealTime, decimal calories, decimal protein, decimal fat, decimal carbs, MealDistribution norm, NormCheckResult result)
        {
            var warnings = new List<string>();

            // Калорийность
            if (calories < norm.Calories * (1 - Tolerance.Calories / 100m))
                warnings.Add($"⚠️ Недостаток калорий: {calories:F0} ккал (норма ≈ {norm.Calories:F0} ккал)");
            else if (calories > norm.Calories * (1 + Tolerance.Calories / 100m))
                warnings.Add($"⚠️ Превышение калорий: {calories:F0} ккал (норма ≈ {norm.Calories:F0} ккал)");
            else if (calories >= norm.Calories * 0.9m && calories <= norm.Calories * 1.1m)
                warnings.Add($"✅ Калорийность в норме: {calories:F0} ккал (норма ≈ {norm.Calories:F0} ккал)");

            // Белки
            if (protein < norm.Protein * (1 - Tolerance.Protein / 100m))
                warnings.Add($"⚠️ Недостаток белка: {protein:F0} г (норма ≈ {norm.Protein:F0} г)");
            else if (protein > norm.Protein * (1 + Tolerance.Protein / 100m))
                warnings.Add($"⚠️ Превышение белка: {protein:F0} г (норма ≈ {norm.Protein:F0} г)");

            // Жиры
            if (fat < norm.Fat * (1 - Tolerance.Fat / 100m))
                warnings.Add($"⚠️ Недостаток жиров: {fat:F0} г (норма ≈ {norm.Fat:F0} г)");
            else if (fat > norm.Fat * (1 + Tolerance.Fat / 100m))
                warnings.Add($"⚠️ Превышение жиров: {fat:F0} г (норма ≈ {norm.Fat:F0} г)");

            // Углеводы
            if (carbs < norm.Carbs * (1 - Tolerance.Carbs / 100m))
                warnings.Add($"⚠️ Недостаток углеводов: {carbs:F0} г (норма ≈ {norm.Carbs:F0} г)");
            else if (carbs > norm.Carbs * (1 + Tolerance.Carbs / 100m))
                warnings.Add($"⚠️ Превышение углеводов: {carbs:F0} г (норма ≈ {norm.Carbs:F0} г)");

            // Соотношение БЖУ (должно быть 1:1:4 для работников фабрики)
            if (protein > 0 && fat > 0 && carbs > 0)
            {
                decimal ratioProtein = protein / protein;
                decimal ratioFat = fat / protein;
                decimal ratioCarbs = carbs / protein;

                if (Math.Abs(ratioFat - 1) > 0.3m)
                    warnings.Add($"ℹ️ Соотношение Б:Ж:У = 1:{ratioFat:F1}:{ratioCarbs:F1} (рекомендуется 1:1:4)");
            }

            if (warnings.Any())
            {
                result.IsValid = false;
                result.Warnings.Add(new NormWarning
                {
                    MealTime = mealTime,
                    Warnings = warnings
                });
            }
        }

        private static void CheckDailyTotals(NormCheckResult result)
        {
            var norm = DefaultNorm;
            var warnings = new List<string>();

            // Калорийность
            if (result.Totals.Calories < norm.Calories * (1 - Tolerance.Calories / 100m))
                warnings.Add($"⚠️ Недостаток калорий за день: {result.Totals.Calories:F0} / {norm.Calories:F0} ккал");
            else if (result.Totals.Calories > norm.Calories * (1 + Tolerance.Calories / 100m))
                warnings.Add($"⚠️ Превышение калорий за день: {result.Totals.Calories:F0} / {norm.Calories:F0} ккал");
            else
                warnings.Add($"✅ Калорийность за день в норме: {result.Totals.Calories:F0} / {norm.Calories:F0} ккал");

            // Белки
            if (result.Totals.Protein < norm.Protein * (1 - Tolerance.Protein / 100m))
                warnings.Add($"⚠️ Недостаток белка за день: {result.Totals.Protein:F0} / {norm.Protein:F0} г");
            else if (result.Totals.Protein > norm.Protein * (1 + Tolerance.Protein / 100m))
                warnings.Add($"⚠️ Превышение белка за день: {result.Totals.Protein:F0} / {norm.Protein:F0} г");
            else
                warnings.Add($"✅ Белки за день в норме: {result.Totals.Protein:F0} / {norm.Protein:F0} г");

            // Жиры
            if (result.Totals.Fat < norm.Fat * (1 - Tolerance.Fat / 100m))
                warnings.Add($"⚠️ Недостаток жиров за день: {result.Totals.Fat:F0} / {norm.Fat:F0} г");
            else if (result.Totals.Fat > norm.Fat * (1 + Tolerance.Fat / 100m))
                warnings.Add($"⚠️ Превышение жиров за день: {result.Totals.Fat:F0} / {norm.Fat:F0} г");
            else
                warnings.Add($"✅ Жиры за день в норме: {result.Totals.Fat:F0} / {norm.Fat:F0} г");

            // Углеводы
            if (result.Totals.Carbs < norm.Carbs * (1 - Tolerance.Carbs / 100m))
                warnings.Add($"⚠️ Недостаток углеводов за день: {result.Totals.Carbs:F0} / {norm.Carbs:F0} г");
            else if (result.Totals.Carbs > norm.Carbs * (1 + Tolerance.Carbs / 100m))
                warnings.Add($"⚠️ Превышение углеводов за день: {result.Totals.Carbs:F0} / {norm.Carbs:F0} г");
            else
                warnings.Add($"✅ Углеводы за день в норме: {result.Totals.Carbs:F0} / {norm.Carbs:F0} г");

            if (warnings.Any())
            {
                result.IsValid = false;
                result.Warnings.Add(new NormWarning
                {
                    MealTime = "За день",
                    Warnings = warnings
                });
            }
        }

        private static decimal CalculateProtein(MenuItemWithDetails item)
{
    var nutrients = NutrientCalculator.CalculateDishNutrients(item.Блюдо_id);
    decimal выход = item.Выход_на_порцию > 0 ? item.Выход_на_порцию : 100m;
    return (nutrients.Белки / 100m) * выход;
}

private static decimal CalculateFat(MenuItemWithDetails item)
{
    var nutrients = NutrientCalculator.CalculateDishNutrients(item.Блюдо_id);
    decimal выход = item.Выход_на_порцию > 0 ? item.Выход_на_порцию : 100m;
    return (nutrients.Жиры / 100m) * выход;
}

private static decimal CalculateCarbs(MenuItemWithDetails item)
{
    var nutrients = NutrientCalculator.CalculateDishNutrients(item.Блюдо_id);
    decimal выход = item.Выход_на_порцию > 0 ? item.Выход_на_порцию : 100m;
    return (nutrients.Углеводы / 100m) * выход;
}

        /// <summary>
        /// Формирует сообщение для отображения пользователю
        /// </summary>
        public static string GetWarningMessage(NormCheckResult result)
        {
            var message = new System.Text.StringBuilder();
            message.AppendLine("═══════════════════════════════════════════════════");
            message.AppendLine("     📋 НОРМОКОНТРОЛЬ ПИЩЕВОЙ ЦЕННОСТИ");
            message.AppendLine("═══════════════════════════════════════════════════");
            message.AppendLine();
            message.AppendLine("Нормативная база:");
            message.AppendLine("• МР 2.3.1.0253-21 (Нормы физиологических потребностей)");
            message.AppendLine("• СанПиН 2.3/2.4.3590-20");
            message.AppendLine("• III группа физической активности (работники фабрики)");
            message.AppendLine();
            message.AppendLine("═══════════════════════════════════════════════════");
            message.AppendLine($"📊 СУТОЧНЫЕ НОРМЫ (3000 ккал):");
            message.AppendLine($"   🍗 Белки: {result.Totals.Protein:F0} / {result.RecommendedTotals.Protein:F0} г");
            message.AppendLine($"   🧈 Жиры: {result.Totals.Fat:F0} / {result.RecommendedTotals.Fat:F0} г");
            message.AppendLine($"   🌾 Углеводы: {result.Totals.Carbs:F0} / {result.RecommendedTotals.Carbs:F0} г");
            message.AppendLine($"   🔥 Калории: {result.Totals.Calories:F0} / {result.RecommendedTotals.Calories:F0} ккал");
            message.AppendLine();

            if (!result.IsValid)
            {
                message.AppendLine("═══════════════════════════════════════════════════");
                message.AppendLine("⚠️ РЕКОМЕНДАЦИИ ПО КОРРЕКТИРОВКЕ МЕНЮ");
                message.AppendLine("═══════════════════════════════════════════════════");
                message.AppendLine();

                foreach (var warning in result.Warnings)
                {
                    message.AppendLine($"▸ {warning.MealTime}:");
                    foreach (var w in warning.Warnings)
                    {
                        message.AppendLine($"     {w}");
                    }
                    message.AppendLine();
                }
            }
            else
            {
                message.AppendLine("═══════════════════════════════════════════════════");
                message.AppendLine("✅ МЕНЮ СООТВЕТСТВУЕТ НОРМАМ");
                message.AppendLine("═══════════════════════════════════════════════════");
            }

            message.AppendLine();
            message.AppendLine("Рекомендации по улучшению рациона:");
            message.AppendLine("• Увеличить долю сложных углеводов (крупы, овощи)");
            message.AppendLine("• Ограничить простые сахара (выпечка, сладости)");
            message.AppendLine("• Поддерживать баланс белков, жиров и углеводов 1:1:4");

            return message.ToString();
        }
    }

    public class DailyNorm
    {
        public int Calories { get; set; }
        public int Protein { get; set; }
        public int Fat { get; set; }
        public int Carbs { get; set; }
    }

    public class MealDistribution
    {
        public int Percentage { get; set; }
        public int Calories { get; set; }
        public int Protein { get; set; }
        public int Fat { get; set; }
        public int Carbs { get; set; }
    }

    public class Deviation
    {
        public int Calories { get; set; }
        public int Protein { get; set; }
        public int Fat { get; set; }
        public int Carbs { get; set; }
    }

    public class NormCheckResult
    {
        public bool IsValid { get; set; }
        public List<NormWarning> Warnings { get; set; }
        public NutrientTotal Totals { get; set; }
        public NutrientTotal RecommendedTotals { get; set; }
    }

    public class NormWarning
    {
        public string MealTime { get; set; }
        public List<string> Warnings { get; set; }
    }

    public class NutrientTotal
    {
        public decimal Calories { get; set; }
        public decimal Protein { get; set; }
        public decimal Fat { get; set; }
        public decimal Carbs { get; set; }
    }
}