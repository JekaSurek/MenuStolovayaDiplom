using System;
using System.Linq;
using System.Windows;
using System.Diagnostics;

namespace MenuStolovaya.Services
{
    public static class CalorieCalculator
    {
        /// <summary>
        /// Рассчитывает калорийность готового блюда на 100г (возвращает в КАЛОРИЯХ)
        /// </summary>
        public static decimal CalculateDishCaloriesPer100g(int dishId)
        {
            try
            {
                using (var db = new MenuStolovayaDBEntities())
                {
                    // Находим технологическую карту для блюда
                    var techCard = db.Технологические_карты
                        .FirstOrDefault(tc => tc.Блюдо_id == dishId);

                    if (techCard == null)
                    {
                        Debug.WriteLine($"Для блюда ID {dishId} не найдена технологическая карта");
                        return 0;
                    }

                    // Получаем все ингредиенты рецептуры
                    var recipeLines = db.Рецептуры
                        .Where(r => r.Технологическая_карта_id == techCard.id)
                        .Join(db.Продукты,
                            r => r.Продукт_id,
                            p => p.id,
                            (r, p) => new
                            {
                                ProductName = p.Наименование,
                                Netto = r.Количество_нетто ?? r.Количество_брутто, // в кг
                                CaloriesPer100g = p.Калорийность ?? 0, // калории на 100г продукта
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

                    // 1. Рассчитываем общую калорийность всех ингредиентов в КАЛОРИЯХ
                    decimal totalCaloriesInCalories = 0;

                    foreach (var line in recipeLines)
                    {
                        // Учитываем потери при обработке
                        decimal weightAfterLossKg = line.Netto *
                            (1 - line.ColdLoss / 100) *
                            (1 - line.HotLoss / 100);

                        // Переводим в граммы
                        decimal weightAfterLossGrams = weightAfterLossKg * 1000;

                        // Калорийность продукта в калориях на грамм
                        decimal caloriesPerGram = line.CaloriesPer100g / 100;

                        // Калорийность ингредиента в калориях
                        decimal ingredientCalories = weightAfterLossGrams * caloriesPerGram;

                        totalCaloriesInCalories += ingredientCalories;

                        Debug.WriteLine($"{line.ProductName}:");
                        Debug.WriteLine($"  Количество: {line.Netto} кг");
                        Debug.WriteLine($"  Вес после потерь: {weightAfterLossGrams:F1} г");
                        Debug.WriteLine($"  Калорийность: {line.CaloriesPer100g} кал/100г");
                        Debug.WriteLine($"  Калорийность ингредиента: {ingredientCalories:F1} кал");
                    }

                    Debug.WriteLine($"ОБЩАЯ калорийность ингредиентов: {totalCaloriesInCalories:F1} кал");

                    // 2. Рассчитываем калорийность на 100г готового блюда
                    decimal outputGrams = techCard.Выход;

                    if (outputGrams <= 0)
                    {
                        Debug.WriteLine("⚠️ Выход блюда не задан!");
                        return 0;
                    }

                    // Формула: (общие_калории / выход_в_граммах) × 100
                    decimal caloriesPer100g = (totalCaloriesInCalories / outputGrams) * 100;

                    Debug.WriteLine($"Калорийность на 100г готового блюда: {caloriesPer100g:F1} кал/100г");
                    Debug.WriteLine($"В ккал: {caloriesPer100g / 1000:F3} ккал/100г");
                    Debug.WriteLine("=== КОНЕЦ РАСЧЕТА ===");

                    return Math.Round(caloriesPer100g, 1);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка расчета калорийности: {ex.Message}");
                return 0;
            }
        }

        /// <summary>
        /// Рассчитывает общую калорийность блюда в ККАЛ
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

                    // Получаем калорийность на 100г в калориях
                    decimal caloriesPer100gInCalories = CalculateDishCaloriesPer100g(dishId);

                    // Переводим в ккал на 100г
                    decimal caloriesPer100gInKcal = caloriesPer100gInCalories / 1000;

                    // Общая калорийность = (калорийность_на_100г / 100) × выход
                    decimal totalCaloriesInKcal = (caloriesPer100gInKcal / 100) * techCard.Выход;

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
                                Netto = r.Количество_нетто ?? r.Количество_брутто,
                                ColdLoss = p.Потери_холодной_обработки ?? 0,
                                HotLoss = p.Потери_горячей_обработки ?? 0
                            })
                        .ToList();

                    if (!recipeLines.Any())
                    {
                        return 0;
                    }

                    decimal totalWeightGrams = 0;

                    foreach (var line in recipeLines)
                    {
                        // Учитываем потери при обработке
                        decimal weightAfterLossKg = line.Netto *
                            (1 - line.ColdLoss / 100) *
                            (1 - line.HotLoss / 100);

                        // Переводим в граммы
                        decimal weightAfterLossGrams = weightAfterLossKg * 1000;
                        totalWeightGrams += weightAfterLossGrams;
                    }

                    // Округляем до 10 грамм
                    decimal outputGrams = Math.Round(totalWeightGrams / 10, MidpointRounding.AwayFromZero) * 10;

                    Debug.WriteLine($"РАСЧЕТ ВЫХОДА для техкарты {technologyCardId}:");
                    Debug.WriteLine($"  Суммарный вес: {totalWeightGrams} г");
                    Debug.WriteLine($"  Рекомендуемый выход: {outputGrams} г");

                    return outputGrams > 0 ? outputGrams : 0;
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
                    if (newOutput > 0)
                    {
                        techCard.Выход = newOutput;
                        Debug.WriteLine($"Обновлен выход: {newOutput} г");
                    }

                    // 2. Рассчитываем и обновляем калорийность на 100г (в калориях)
                    decimal newCaloriesPer100g = CalculateDishCaloriesPer100g(techCard.Блюдо_id);

                    var dish = db.Блюда.Find(techCard.Блюдо_id);
                    if (dish != null)
                    {
                        dish.Калорийность_расчетная = newCaloriesPer100g;
                        dish.Выход_стандартный = newOutput;

                        Debug.WriteLine($"Обновлена калорийность блюда '{dish.Наименование}':");
                        Debug.WriteLine($"  В калориях на 100г: {newCaloriesPer100g} кал/100г");
                        Debug.WriteLine($"  В ккал на 100г: {newCaloriesPer100g / 1000:F3} ккал/100г");
                        Debug.WriteLine($"  Общая калорийность: {CalculateTotalDishCaloriesInKcal(dish.id):F2} ккал");
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
        /// Обновляет калорийность всех блюд (используется кнопкой в интерфейсе)
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

                    Debug.WriteLine($"=== НАЧАЛО ОБНОВЛЕНИЯ КАЛОРИЙНОСТИ ВСЕХ БЛЮД ===");
                    Debug.WriteLine($"Всего блюд: {dishes.Count}");

                    foreach (var dish in dishes)
                    {
                        try
                        {
                            Debug.WriteLine($"\n--- Обработка блюда: {dish.Наименование} (ID: {dish.id}) ---");

                            decimal oldCalories = dish.Калорийность_расчетная ?? 0;
                            decimal newCalories = CalculateDishCaloriesPer100g(dish.id);

                            Debug.WriteLine($"Старая калорийность: {oldCalories}");
                            Debug.WriteLine($"Новая калорийность: {newCalories}");

                            if (Math.Abs(oldCalories - newCalories) > 0.1m || dish.Калорийность_расчетная == null)
                            {
                                dish.Калорийность_расчетная = newCalories;
                                updatedCount++;
                                Debug.WriteLine($"✓ Обновлено");
                            }
                            else
                            {
                                Debug.WriteLine($"- Без изменений");
                            }
                        }
                        catch (Exception ex)
                        {
                            errorCount++;
                            Debug.WriteLine($"✗ Ошибка: {ex.Message}");
                        }
                    }

                    db.SaveChanges();

                    string message = $"Калорийность обновлена для {updatedCount} блюд из {dishes.Count}";
                    if (errorCount > 0)
                    {
                        message += $"\nОшибок: {errorCount}";
                    }

                    MessageBox.Show(
                        message,
                        "Обновление завершено",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Ошибка при обновлении калорийности: {ex.Message}",
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
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
                                Netto = r.Количество_нетто ?? r.Количество_брутто,
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

                    decimal totalCaloriesCal = 0;
                    decimal totalWeightGrams = 0;

                    result += "Ингредиенты:\n";

                    foreach (var line in recipeLines)
                    {
                        decimal weightAfterLossKg = line.Netto *
                            (1 - line.ColdLoss / 100) *
                            (1 - line.HotLoss / 100);

                        decimal weightAfterLossGrams = weightAfterLossKg * 1000;
                        totalWeightGrams += weightAfterLossGrams;

                        decimal caloriesPerGram = line.CaloriesPer100g / 100;
                        decimal ingredientCalories = weightAfterLossGrams * caloriesPerGram;
                        totalCaloriesCal += ingredientCalories;

                        result += $"{line.ProductName}:\n";
                        result += $"  Кол-во нетто: {line.Netto} кг\n";
                        result += $"  Вес с учетом потерь: {weightAfterLossGrams:F1} г\n";
                        result += $"  Калорийность продукта: {line.CaloriesPer100g} кал/100г\n";
                        result += $"  Калории в ингредиенте: {ingredientCalories:F1} кал\n\n";
                    }

                    decimal expectedOutputGrams = totalWeightGrams;
                    decimal expectedCaloriesPer100g = totalWeightGrams > 0 ?
                        (totalCaloriesCal / totalWeightGrams) * 100 : 0;

                    result += $"ИТОГО:\n";
                    result += $"  Общий вес с учетом потерь: {totalWeightGrams:F1} г\n";
                    result += $"  Общие калории: {totalCaloriesCal:F1} кал\n";
                    result += $"  Ожидаемый выход: {expectedOutputGrams:F1} г\n";
                    result += $"  Ожидаемая калорийность: {expectedCaloriesPer100g:F1} кал/100г\n";
                    result += $"  В ккал: {expectedCaloriesPer100g / 1000:F3} ккал/100г\n";

                    decimal actualCalories = CalculateDishCaloriesPer100g(dishId);
                    result += $"  Фактическая калорийность: {actualCalories:F1} кал/100г\n";
                    result += $"  Фактический выход: {techCard.Выход} г\n";

                    if (Math.Abs(expectedOutputGrams - techCard.Выход) > 10)
                        result += $"⚠️ РАСХОЖДЕНИЕ ВЫХОДА!\n";

                    return result;
                }
            }
            catch (Exception ex)
            {
                return $"Ошибка при проверке: {ex.Message}";
            }
        }

        /// <summary>
        /// Рассчитывает калорийность продукта из БЖУ (в калориях)
        /// </summary>
        public static decimal CalculateProductCalories(decimal? proteins, decimal? fats, decimal? carbohydrates)
        {
            if (!proteins.HasValue || !fats.HasValue || !carbohydrates.HasValue)
                return 0;

            // Стандартная формула: 4 ккал/г белков, 9 ккал/г жиров, 4 ккал/г углеводов
            decimal caloriesInKcal = proteins.Value * 4 + fats.Value * 9 + carbohydrates.Value * 4;
            return caloriesInKcal * 1000; // Возвращаем в калориях
        }
    }
}