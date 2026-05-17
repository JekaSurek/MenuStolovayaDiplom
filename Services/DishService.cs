using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using MenuStolovaya.Models;

namespace MenuStolovaya.Services
{
    public class DishService
    {
        public List<DishDisplay> GetDishes(string filter = "")
        {
            try
            {
                using (var db = new MenuStolovayaDBEntities())
                {
                    var query = db.Блюда
                        .Include("Виды_блюд")
                        .Include("Пользователи")
                        .Where(d => d.Активно == true); // Только активные блюда

                    if (!string.IsNullOrWhiteSpace(filter))
                    {
                        query = query.Where(d =>
                            d.Наименование.Contains(filter) ||
                            d.Полное_наименование.Contains(filter) ||
                            (d.Виды_блюд != null && d.Виды_блюд.Наименование.Contains(filter)));
                    }

                    var dishes = query.ToList();

                    var result = new List<DishDisplay>();

                    foreach (var dish in dishes)
                    {
                        string автор = "Неизвестно";
                        if (dish.Пользователи != null)
                        {
                            автор = $"{dish.Пользователи.Фамилия} {dish.Пользователи.Имя}";
                        }

                        string видБлюда = "Не указано";
                        if (dish.Виды_блюд != null)
                        {
                            видБлюда = dish.Виды_блюд.Наименование;
                        }

                        result.Add(new DishDisplay
                        {
                            Id = dish.id,
                            Наименование = dish.Наименование,
                            Полное_наименование = dish.Полное_наименование ?? string.Empty,
                            Вид_блюда = видБлюда,
                            Выход_стандартный = dish.Выход_стандартный ?? 100,
                            Время_приготовления = dish.Время_приготовления ?? 30,
                            Калорийность_расчетная = dish.Калорийность_расчетная,
                            Автор = автор,
                            Дата_создания = dish.Дата_создания ?? DateTime.Now
                        });
                    }

                    return result;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке блюд: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return new List<DishDisplay>();
            }
        }

        public bool AddDish(DishModel dish)
        {
            try
            {
                using (var db = new MenuStolovayaDBEntities())
                {
                    // Проверка на дубликаты
                    if (db.Блюда.Any(d => d.Наименование == dish.Наименование && d.Активно == true))
                    {
                        MessageBox.Show("Блюдо с таким наименованием уже существует", "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                        return false;
                    }

                    var newDish = new Блюда
                    {
                        Наименование = dish.Наименование,
                        Полное_наименование = dish.Полное_наименование,
                        Вид_блюда_id = dish.Вид_блюда_id,
                        Выход_стандартный = dish.Выход_стандартный,
                        Время_приготовления = dish.Время_приготовления,
                        Калорийность_расчетная = dish.Калорийность_расчетная,
                        Активно = true,
                        Дата_создания = DateTime.Now,
                        Кто_создал_id = Models.ThisUser.CurrentUser?.Id ?? 1
                    };

                    db.Блюда.Add(newDish);
                    db.SaveChanges();
                    return true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при добавлении блюда: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        public bool UpdateDish(DishModel dish)
        {
            try
            {
                using (var db = new MenuStolovayaDBEntities())
                {
                    var existingDish = db.Блюда.Find(dish.Id);
                    if (existingDish != null && existingDish.Активно == true)
                    {
                        // Проверка на дубликаты (исключая текущее блюдо)
                        if (db.Блюда.Any(d => d.Наименование == dish.Наименование && d.id != dish.Id && d.Активно == true))
                        {
                            MessageBox.Show("Блюдо с таким наименованием уже существует", "Ошибка",
                                MessageBoxButton.OK, MessageBoxImage.Error);
                            return false;
                        }

                        existingDish.Наименование = dish.Наименование;
                        existingDish.Полное_наименование = dish.Полное_наименование;
                        existingDish.Вид_блюда_id = dish.Вид_блюда_id;
                        existingDish.Выход_стандартный = dish.Выход_стандартный;
                        existingDish.Время_приготовления = dish.Время_приготовления;

                        // Обновляем калорийность только если она предоставлена
                        if (dish.Калорийность_расчетная.HasValue)
                        {
                            existingDish.Калорийность_расчетная = dish.Калорийность_расчетная;
                        }

                        db.SaveChanges();
                        return true;
                    }
                    return false;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при обновлении блюда: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        public bool DeleteDish(int dishId)
        {
            try
            {
                using (var db = new MenuStolovayaDBEntities())
                {
                    var dish = db.Блюда.Find(dishId);
                    if (dish != null && dish.Активно == true)
                    {
                        // Проверяем, где используется блюдо
                        var menuItems = db.Строки_меню.Where(sm => sm.Блюдо_id == dishId).ToList();
                        var techCards = db.Технологические_карты.Where(tc => tc.Блюдо_id == dishId).ToList();

                        // Проверяем, есть ли утвержденные технологические карты или калькуляционные карточки
                        bool hasApprovedTechCards = techCards.Any(tc => tc.Статус == "Утверждена");

                        // Запрашиваем подтверждение с информацией о последствиях
                        string message = $"Вы уверены, что хотите удалить блюдо \"{dish.Наименование}\"?\n\n";

                        if (menuItems.Any())
                        {
                            message += $"• Будет удалено из {menuItems.Count} меню\n";
                        }

                        if (techCards.Any())
                        {
                            message += $"• Будет удалено {techCards.Count} технологических карт\n";
                        }

                        if (hasApprovedTechCards)
                        {
                            message += "\n⚠️ Внимание! Будут удалены утвержденные технологические карты!\n";
                        }

                        message += "\nБлюдо будет помечено как неактивное.";

                        var result = MessageBox.Show(message, "Подтверждение удаления",
                            MessageBoxButton.YesNo, MessageBoxImage.Warning);

                        if (result != MessageBoxResult.Yes)
                        {
                            return false;
                        }

                        // Удаляем из меню
                        if (menuItems.Any())
                        {
                            foreach (var menuItem in menuItems)
                            {
                                db.Строки_меню.Remove(menuItem);
                            }
                        }

                        // Удаляем технологические карты и все связанные данные
                        if (techCards.Any())
                        {
                            foreach (var techCard in techCards)
                            {
                                // Удаляем рецептуры
                                var recipes = db.Рецептуры.Where(r => r.Технологическая_карта_id == techCard.id).ToList();
                                if (recipes.Any())
                                {
                                    foreach (var recipe in recipes)
                                    {
                                        db.Рецептуры.Remove(recipe);
                                    }
                                }

                                // Удаляем калькуляционные карточки
                                var calcCards = db.Калькуляционные_карточки.Where(cc => cc.Технологическая_карта_id == techCard.id).ToList();
                                foreach (var calcCard in calcCards)
                                {
                                    // Удаляем строки калькуляции
                                    var calcLines = db.Строки_калькуляции.Where(sc => sc.Калькуляционная_карточка_id == calcCard.id).ToList();
                                    if (calcLines.Any())
                                    {
                                        foreach (var line in calcLines)
                                        {
                                            db.Строки_калькуляции.Remove(line);
                                        }
                                    }

                                    db.Калькуляционные_карточки.Remove(calcCard);
                                }

                                db.Технологические_карты.Remove(techCard);
                            }
                        }

                        // Мягкое удаление блюда
                        dish.Активно = false;
                        db.SaveChanges();

                        MessageBox.Show($"Блюдо \"{dish.Наименование}\" успешно удалено", "Успех",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                        return true;
                    }

                    MessageBox.Show("Блюдо не найдено или уже удалено", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return false;
                }
            }
            catch (System.Data.Entity.Infrastructure.DbUpdateException dbEx)
            {
                string errorMessage = "Ошибка при удалении блюда:\n";

                if (dbEx.InnerException != null && dbEx.InnerException.Message.Contains("REFERENCE constraint"))
                {
                    errorMessage += "Существуют связанные записи, которые не могут быть удалены.\n" +
                        "Возможно, есть утвержденные калькуляционные карточки.";
                }
                else
                {
                    errorMessage += dbEx.InnerException?.Message ?? dbEx.Message;
                }

                MessageBox.Show(errorMessage, "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при удалении блюда: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }
    }

    public class DishDisplay
    {
        public int Id { get; set; }
        public string Наименование { get; set; }
        public string Полное_наименование { get; set; }
        public string Вид_блюда { get; set; }
        public decimal Выход_стандартный { get; set; }
        public int Время_приготовления { get; set; }

        // Калорийность блюда на 100г (в калориях)
        public decimal? Калорийность_расчетная { get; set; }

        // Общая калорийность блюда в ккал
        public decimal Калорийность_общая
        {
            get
            {
                if (Калорийность_расчетная.HasValue && Выход_стандартный > 0)
                {
                    // Калорийность_расчетная - калории на 100г готового блюда
                    // 1. Переводим в ккал на 100г
                    decimal caloriesPer100gInKcal = Калорийность_расчетная.Value / 1000;

                    // 2. Калорийность на 1 грамм в ккал
                    decimal caloriesPerGramInKcal = caloriesPer100gInKcal / 100;

                    // 3. Умножаем на выход блюда в граммах
                    decimal totalCaloriesInKcal = caloriesPerGramInKcal * Выход_стандартный;

                    return Math.Round(totalCaloriesInKcal, 2);
                }
                return 0;
            }
        }

        // Калорийность на 100г в ккал (столбец Ккал/100г)
        public string Калорийность_на_100г
        {
            get
            {
                if (Калорийность_расчетная.HasValue)
                {
                    // Калорийность_расчетная - калории на 100г
                    // Переводим в ккал
                    decimal caloriesPer100gInKcal = Калорийность_расчетная.Value / 1000;
                    return $"{caloriesPer100gInKcal:F3} ккал/100г";
                }
                return "0 ккал/100г";
            }
        }

        public string Автор { get; set; }
        public DateTime Дата_создания { get; set; }
    }
}