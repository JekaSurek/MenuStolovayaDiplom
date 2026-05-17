using System;
using System.Linq;
using System.Windows;
using System.Diagnostics;

namespace MenuStolovaya.Services
{
    public static class CalorieCalculator
    {
        /// <summary>
        /// Рассчитывает калорийность готового блюда на 100г (возвращает в КИЛОКАЛОРИЯХ)
        /// </summary>
        public static decimal CalculateDishCaloriesPer100g(int dishId)
        {
            try
            {
                using (var db = new MenuStolovayaDBEntities())
                {
                    var techCard = db.Технологические_карты
                        .FirstOrDefault(tc => tc.Блюдо_id == dishId);

                    if (techCard == null)
                    {
                        Debug.WriteLine($"Для блюда ID {dishId} не найдена технологическая карта");
                        return 0;
                    }

                    var recipeLines = db.Рецептуры
                        .Where(r => r.Технологическая_карта_id == techCard.id)
                        .Join(db.Продукты,
                            r => r.Продукт_id,
                            p => p.id,
                            (r, p) => new
                            {
                                ProductName = p.Наименование,
                                Brutto = r.Количество_брутто, // уже в кг
                                Netto = r.Количество_нетто ?? r.Количество_брутто,
                                CaloriesPer100g = p.Калорийность ?? 0,
                                ColdLoss = p.Потери_холодной_обработки ?? 0,
                                HotLoss = p.Потери_горячей_обработки ?? 0
                            })
                        .ToList();

                    if (!recipeLines.Any())
                    {
                        Debug.WriteLine($"Для техкарты {techCard.Номер} нет ингредиентов");
                        return 0;
                    }

                    Debug.WriteLine($"=== РАСЧЕТ КАЛОРИЙНОСТИ ДЛЯ БЛЮДА ID {dishId} ===");
                    Debug.WriteLine($"Техкарта: {techCard.Номер}, Выход: {techCard.Выход} г");

                    // 1. Рассчитываем общую калорийность всех ингредиентов (в КИЛОКАЛОРИЯХ)
                    decimal totalCaloriesInKcal = 0;
                    decimal totalWeightAfterLossGrams = 0;

                    foreach (var line in recipeLines)
                    {
                        // Учитываем потери при обработке (потери применяются к БРУТТО)
                        decimal weightAfterLossKg = line.Brutto *
                            (1 - line.ColdLoss / 100) *
                            (1 - line.HotLoss / 100);

                        decimal weightAfterLossGrams = weightAfterLossKg * 1000;
                        totalWeightAfterLossGrams += weightAfterLossGrams;

                        // Калорийность продукта в ккал на грамм
                        decimal caloriesPerGramInKcal = line.CaloriesPer100g / 100;

                        // Калорийность ингредиента в ккал
                        decimal ingredientCaloriesInKcal = weightAfterLossGrams * caloriesPerGramInKcal;
                        totalCaloriesInKcal += ingredientCaloriesInKcal;

                        Debug.WriteLine($"{line.ProductName}:");
                        Debug.WriteLine($"  Брутто: {line.Brutto} кг");
                        Debug.WriteLine($"  Вес после потерь: {weightAfterLossGrams:F1} г");
                        Debug.WriteLine($"  Калорийность: {line.CaloriesPer100g} ккал/100г");
                        Debug.WriteLine($"  Калорийность ингредиента: {ingredientCaloriesInKcal:F2} ккал");
                    }

                    Debug.WriteLine($"ОБЩИЙ ВЕС после потерь: {totalWeightAfterLossGrams:F1} г");
                    Debug.WriteLine($"ОБЩАЯ калорийность: {totalCaloriesInKcal:F2} ккал");

                    // 2. Рассчитываем калорийность на 100г готового блюда
                    decimal actualOutputGrams = techCard.Выход;

                    if (actualOutputGrams <= 0)
                    {
                        Debug.WriteLine("⚠️ Выход блюда не задан, используем расчётный вес");
                        actualOutputGrams = totalWeightAfterLossGrams;
                    }

                    // Формула: (общие_ккал / выход_в_граммах) × 100
                    decimal caloriesPer100gInKcal = (totalCaloriesInKcal / actualOutputGrams) * 100;

                    Debug.WriteLine($"Калорийность на 100г готового блюда: {caloriesPer100gInKcal:F2} ккал/100г");
                    Debug.WriteLine($"Общая калорийность блюда: {totalCaloriesInKcal:F2} ккал");
                    Debug.WriteLine("=== КОНЕЦ РАСЧЕТА ===");

                    return Math.Round(caloriesPer100gInKcal, 1);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка расчета калорийности: {ex.Message}");
                return 0;
            }
        }

        /// <summary>
        /// Рассчитывает общую калорийность блюда в ККАЛ (для всего веса)
        /// </summary>
        public static decimal CalculateTotalDishCaloriesInKcal(int dishId)
        {
            try
            {
                using (var db = new MenuStolovayaDBEntities())
                {
                    var techCard = db.Технологические_карты
                        .FirstOrDefault(tc => tc.Блюдо_id == dishId);

                    if (techCard == null) return 0;

                    var recipeLines = db.Рецептуры
                        .Where(r => r.Технологическая_карта_id == techCard.id)
                        .Join(db.Продукты,
                            r => r.Продукт_id,
                            p => p.id,
                            (r, p) => new
                            {
                                Brutto = r.Количество_брутто,
                                CaloriesPer100g = p.Калорийность ?? 0,
                                ColdLoss = p.Потери_холодной_обработки ?? 0,
                                HotLoss = p.Потери_горячей_обработки ?? 0
                            })
                        .ToList();

                    if (!recipeLines.Any()) return 0;

                    decimal totalCaloriesInKcal = 0;

                    foreach (var line in recipeLines)
                    {
                        decimal weightAfterLossKg = line.Brutto *
                            (1 - line.ColdLoss / 100) *
                            (1 - line.HotLoss / 100);

                        decimal weightAfterLossGrams = weightAfterLossKg * 1000;
                        decimal caloriesPerGramInKcal = line.CaloriesPer100g / 100;
                        decimal ingredientCalories = weightAfterLossGrams * caloriesPerGramInKcal;

                        totalCaloriesInKcal += ingredientCalories;
                    }

                    return Math.Round(totalCaloriesInKcal, 2);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка расчета общей калорийности: {ex.Message}");
                return 0;
            }
        }

        /// <summary>
        /// Рассчитывает выход блюда на основе рецептуры
        /// </summary>
        public static decimal CalculateDishOutput(int technologyCardId)
        {
            try
            {
                using (var db = new MenuStolovayaDBEntities())
                {
                    var recipeLines = db.Рецептуры
                        .Where(r => r.Технологическая_карта_id == technologyCardId)
                        .Join(db.Продукты,
                            r => r.Продукт_id,
                            p => p.id,
                            (r, p) => new
                            {
                                Brutto = r.Количество_брутто,
                                ColdLoss = p.Потери_холодной_обработки ?? 0,
                                HotLoss = p.Потери_горячей_обработки ?? 0
                            })
                        .ToList();

                    if (!recipeLines.Any()) return 0;

                    decimal totalWeightGrams = 0;

                    foreach (var line in recipeLines)
                    {
                        decimal weightAfterLossKg = line.Brutto *
                            (1 - line.ColdLoss / 100) *
                            (1 - line.HotLoss / 100);

                        totalWeightGrams += weightAfterLossKg * 1000;
                    }

                    // Округляем до 10 грамм
                    decimal outputGrams = Math.Round(totalWeightGrams / 10, MidpointRounding.AwayFromZero) * 10;

                    Debug.WriteLine($"РАСЧЕТ ВЫХОДА для техкарты {technologyCardId}:");
                    Debug.WriteLine($"  Суммарный вес: {totalWeightGrams:F1} г");
                    Debug.WriteLine($"  Рекомендуемый выход: {outputGrams:F0} г");

                    return outputGrams > 0 ? outputGrams : Math.Round(totalWeightGrams, 0);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка расчета выхода: {ex.Message}");
                return 0;
            }
        }

        /// <summary>
        /// Автоматически обновляет выход и калорийность блюда при изменении рецептуры
        /// </summary>
        public static void UpdateDishCalculations(int technologyCardId)
        {
            try
            {
                using (var db = new MenuStolovayaDBEntities())
                {
                    var techCard = db.Технологические_карты.Find(technologyCardId);
                    if (techCard == null) return;

                    Debug.WriteLine($"=== АВТООБНОВЛЕНИЕ ДЛЯ ТЕХКАРТЫ {techCard.Номер} ===");

                    // 1. Рассчитываем и обновляем выход
                    decimal newOutput = CalculateDishOutput(technologyCardId);
                    if (newOutput > 0 && newOutput != techCard.Выход)
                    {
                        techCard.Выход = newOutput;
                        Debug.WriteLine($"Обновлен выход: {newOutput:F0} г");
                    }

                    db.SaveChanges();

                    // 2. Рассчитываем и обновляем калорийность на 100г
                    decimal newCaloriesPer100g = CalculateDishCaloriesPer100g(techCard.Блюдо_id);

                    var dish = db.Блюда.Find(techCard.Блюдо_id);
                    if (dish != null && dish.Калорийность_расчетная != newCaloriesPer100g)
                    {
                        dish.Калорийность_расчетная = newCaloriesPer100g;
                        Debug.WriteLine($"Обновлена калорийность блюда '{dish.Наименование}':");
                        Debug.WriteLine($"  {newCaloriesPer100g:F1} ккал/100г");

                        decimal totalCalories = CalculateTotalDishCaloriesInKcal(dish.id);
                        Debug.WriteLine($"  Общая калорийность блюда: {totalCalories:F2} ккал");
                    }

                    db.SaveChanges();
                    Debug.WriteLine("=== АВТООБНОВЛЕНИЕ ЗАВЕРШЕНО ===");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка при автообновлении: {ex.Message}");
            }
        }

        /// <summary>
        /// Обновляет калорийность всех блюд
        /// </summary>
        public static void UpdateAllDishesCalories()
        {
            try
            {
                using (var db = new MenuStolovayaDBEntities())
                {
                    var dishes = db.Блюда
                        .Where(d => d.Активно == true)
                        .ToList();

                    int updatedCount = 0;
                    int errorCount = 0;

                    foreach (var dish in dishes)
                    {
                        try
                        {
                            var techCard = db.Технологические_карты
                                .FirstOrDefault(tc => tc.Блюдо_id == dish.id);

                            if (techCard != null)
                            {
                                decimal newCalories = CalculateDishCaloriesPer100g(dish.id);

                                if (Math.Abs((dish.Калорийность_расчетная ?? 0) - newCalories) > 0.1m)
                                {
                                    dish.Калорийность_расчетная = newCalories;
                                    updatedCount++;
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            errorCount++;
                            Debug.WriteLine($"Ошибка для блюда {dish.id}: {ex.Message}");
                        }
                    }

                    db.SaveChanges();

                    MessageBox.Show(
                        $"Калорийность обновлена для {updatedCount} блюд.\nОшибок: {errorCount}",
                        "Обновление завершено",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Проверяет корректность расчетов для конкретного блюда
        /// </summary>
        public static string VerifyCalculations(int dishId)
        {
            try
            {
                using (var db = new MenuStolovayaDBEntities())
                {
                    var techCard = db.Технологические_карты
                        .FirstOrDefault(tc => tc.Блюдо_id == dishId);

                    if (techCard == null)
                        return "Технологическая карта не найдена";

                    var recipeLines = db.Рецептуры
                        .Where(r => r.Технологическая_карта_id == techCard.id)
                        .Join(db.Продукты,
                            r => r.Продукт_id,
                            p => p.id,
                            (r, p) => new
                            {
                                ProductName = p.Наименование,
                                Brutto = r.Количество_брутто,
                                CaloriesPer100g = p.Калорийность ?? 0,
                                ColdLoss = p.Потери_холодной_обработки ?? 0,
                                HotLoss = p.Потери_горячей_обработки ?? 0
                            })
                        .ToList();

                    if (!recipeLines.Any())
                        return "Рецептура пуста";

                    string result = $"=== ПРОВЕРКА РАСЧЕТОВ ===\n";
                    result += $"Техкарта: {techCard.Номер}\n";
                    result += $"Текущий выход: {techCard.Выход} г\n\n";

                    decimal totalCaloriesKcal = 0;
                    decimal totalWeightGrams = 0;

                    result += "Ингредиенты:\n";

                    foreach (var line in recipeLines)
                    {
                        decimal weightAfterLossKg = line.Brutto *
                            (1 - line.ColdLoss / 100) *
                            (1 - line.HotLoss / 100);

                        decimal weightAfterLossGrams = weightAfterLossKg * 1000;
                        totalWeightGrams += weightAfterLossGrams;

                        decimal caloriesPerGramInKcal = line.CaloriesPer100g / 100;
                        decimal ingredientCalories = weightAfterLossGrams * caloriesPerGramInKcal;
                        totalCaloriesKcal += ingredientCalories;

                        result += $"{line.ProductName}:\n";
                        result += $"  Брутто: {line.Brutto:F3} кг\n";
                        result += $"  Вес после потерь: {weightAfterLossGrams:F1} г\n";
                        result += $"  Калорийность: {line.CaloriesPer100g:F1} ккал/100г\n";
                        result += $"  Ккал в ингредиенте: {ingredientCalories:F2} ккал\n\n";
                    }

                    result += $"ИТОГО:\n";
                    result += $"  Общий вес после потерь: {totalWeightGrams:F1} г\n";
                    result += $"  Общая калорийность: {totalCaloriesKcal:F2} ккал\n";
                    result += $"  Калорийность на 100г: {(totalCaloriesKcal / techCard.Выход) * 100:F1} ккал/100г\n";

                    result += $"\nТЕКУЩИЕ ЗНАЧЕНИЯ В БАЗЕ:\n";
                    result += $"  Выход: {techCard.Выход} г\n";

                    var dish = db.Блюда.Find(dishId);
                    if (dish != null)
                    {
                        result += $"  Калорийность на 100г: {dish.Калорийность_расчетная:F1} ккал/100г\n";
                    }

                    return result;
                }
            }
            catch (Exception ex)
            {
                return $"Ошибка при проверке: {ex.Message}";
            }
        }

        /// <summary>
        /// Рассчитывает калорийность продукта из БЖУ (в КИЛОКАЛОРИЯХ)
        /// </summary>
        public static decimal CalculateProductCalories(decimal? proteins, decimal? fats, decimal? carbohydrates)
        {
            if (!proteins.HasValue || !fats.HasValue || !carbohydrates.HasValue)
                return 0;

            return (proteins.Value * 4) + (fats.Value * 9) + (carbohydrates.Value * 4);
        }
    }
}