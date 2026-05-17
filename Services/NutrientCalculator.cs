using System;
using System.Linq;
using MenuStolovaya.Models;

namespace MenuStolovaya.Services
{
    public static class NutrientCalculator
    {
        public static (decimal Белки, decimal Жиры, decimal Углеводы, decimal Калории) CalculateDishNutrients(int dishId)
        {
            try
            {
                using (var db = new MenuStolovayaDBEntities())
                {
                    var techCard = db.Технологические_карты
                        .FirstOrDefault(tc => tc.Блюдо_id == dishId);

                    if (techCard == null) return (0, 0, 0, 0);

                    var recipes = db.Рецептуры
                        .Where(r => r.Технологическая_карта_id == techCard.id)
                        .Join(db.Продукты,
                            r => r.Продукт_id,
                            p => p.id,
                            (r, p) => new
                            {
                                ВесНетто = r.Количество_нетто ?? r.Количество_брутто, // в кг
                                Белки = p.Белки ?? 0, // г/100г
                                Жиры = p.Жиры ?? 0, // г/100г
                                Углеводы = p.Углеводы ?? 0, // г/100г
                                ХолодныеПотери = p.Потери_холодной_обработки ?? 0,
                                ГорячиеПотери = p.Потери_горячей_обработки ?? 0
                            })
                        .ToList();

                    decimal белки_г = 0;
                    decimal жиры_г = 0;
                    decimal углеводы_г = 0;

                    foreach (var recipe in recipes)
                    {
                        // Учитываем потери
                        decimal весПослеПотерьКг = recipe.ВесНетто *
                            (1 - recipe.ХолодныеПотери / 100) *
                            (1 - recipe.ГорячиеПотери / 100);

                        // Переводим в граммы
                        decimal весПослеПотерьГ = весПослеПотерьКг * 1000;

                        // Рассчитываем БЖУ для ингредиента
                        белки_г += (recipe.Белки / 100) * весПослеПотерьГ;
                        жиры_г += (recipe.Жиры / 100) * весПослеПотерьГ;
                        углеводы_г += (recipe.Углеводы / 100) * весПослеПотерьГ;
                    }

                    // Рассчитываем калории (в калориях, не ккал!)
                    decimal калории = (белки_г * 4 + жиры_г * 9 + углеводы_г * 4);

                    // Рассчитываем на 100г готового блюда
                    if (techCard.Выход > 0)
                    {
                        decimal коэффициент = 100 / techCard.Выход;
                        return (белки_г * коэффициент, жиры_г * коэффициент,
                                углеводы_г * коэффициент, калории * коэффициент);
                    }

                    return (0, 0, 0, 0);
                }
            }
            catch
            {
                return (0, 0, 0, 0);
            }
        }

        public static string GetNutrientDescription(int dishId)
        {
            var nutrients = CalculateDishNutrients(dishId);

            // Переводим калории в ккал
            decimal калорииВКкал = nutrients.Калории / 1000;

            return $"Б: {nutrients.Белки:F1} г | Ж: {nutrients.Жиры:F1} г | У: {nutrients.Углеводы:F1} г | {калорииВКкал:F1} калорий/100г";
        }
    }
}