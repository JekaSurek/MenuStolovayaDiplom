using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using MenuStolovaya.Models;

namespace MenuStolovaya.Services
{
    public class TechnologyCardService
    {
        public List<TechnologyCardDisplay> GetTechnologyCards(string filter = "")
        {
            try
            {
                using (var db = new MenuStolovayaDBEntities())
                {
                    var query = db.Технологические_карты
                        .Include("Блюда")
                        .Include("Пользователи")
                        .Include("Блюда.Виды_блюд")
                        .AsQueryable();

                    if (!string.IsNullOrWhiteSpace(filter))
                    {
                        query = query.Where(tc =>
                            tc.Номер.Contains(filter) ||
                            tc.Блюда.Наименование.Contains(filter) ||
                            (tc.Блюда.Виды_блюд != null && tc.Блюда.Виды_блюд.Наименование.Contains(filter)) ||
                            tc.Технология_приготовления.Contains(filter));
                    }

                    var cards = query.ToList();
                    var result = new List<TechnologyCardDisplay>();

                    foreach (var tc in cards)
                    {
                        string утвердил = "Не утверждена";
                        if (tc.Кто_утвердил_id != null)
                        {
                            var user = db.Пользователи.Find(tc.Кто_утвердил_id);
                            утвердил = user != null ? $"{user.Фамилия} {user.Имя}" : "Неизвестно";
                        }

                        int количествоИнгредиентов = db.Рецептуры.Count(r => r.Технологическая_карта_id == tc.id);

                        // Просто показываем выход из техкарты (без сравнения)
                        string outputInfo = $"{tc.Выход:N1} г";

                        result.Add(new TechnologyCardDisplay
                        {
                            Id = tc.id,
                            Номер = tc.Номер,
                            Блюдо = tc.Блюда?.Наименование ?? "Неизвестно",
                            Блюдо_id = tc.Блюдо_id,
                            Выход = tc.Выход,
                            Выход_отформатированный = outputInfo,
                            Статус = tc.Статус,
                            Дата_создания = tc.Дата_создания ?? DateTime.Now,
                            Количество_ингредиентов = количествоИнгредиентов,
                            Кто_утвердил = утвердил
                        });
                    }

                    return result;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке технологических карт: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return new List<TechnologyCardDisplay>();
            }
        }

        public TechnologyCardModel GetTechnologyCardById(int id)
        {
            try
            {
                using (var db = new MenuStolovayaDBEntities())
                {
                    var tc = db.Технологические_карты
                        .Include("Блюда")
                        .FirstOrDefault(t => t.id == id);

                    if (tc != null)
                    {
                        return new TechnologyCardModel
                        {
                            Id = tc.id,
                            Номер = tc.Номер,
                            Блюдо_id = tc.Блюдо_id,
                            Выход = tc.Выход,
                            Технология_приготовления = tc.Технология_приготовления,
                            Дата_создания = tc.Дата_создания ?? DateTime.Now,
                            Статус = tc.Статус,
                            Кто_утвердил_id = tc.Кто_утвердил_id,
                            Дата_утверждения = tc.Дата_утверждения
                        };
                    }
                    return null;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при получении технологической карты: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }
        }

        public bool AddTechnologyCard(TechnologyCardModel technologyCard)
        {
            try
            {
                using (var db = new MenuStolovayaDBEntities())
                {
                    // Генерация номера
                    string номер = GenerateCardNumber();

                    var newCard = new Технологические_карты
                    {
                        Номер = номер,
                        Блюдо_id = technologyCard.Блюдо_id,
                        Выход = technologyCard.Выход,
                        Технология_приготовления = technologyCard.Технология_приготовления,
                        Дата_создания = DateTime.Now,
                        Статус = "Черновик"
                    };

                    db.Технологические_карты.Add(newCard);
                    db.SaveChanges();

                    // После создания техкарты можно автоматически обновить блюдо
                    if (newCard.Выход > 0)
                    {
                        var dish = db.Блюда.Find(technologyCard.Блюдо_id);
                        if (dish != null)
                        {
                            dish.Выход_стандартный = newCard.Выход;
                            db.SaveChanges();
                        }
                    }

                    return true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при добавлении технологической карты: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        public bool UpdateTechnologyCard(TechnologyCardModel technologyCard)
        {
            try
            {
                using (var db = new MenuStolovayaDBEntities())
                {
                    var existingCard = db.Технологические_карты.Find(technologyCard.Id);
                    if (existingCard != null)
                    {
                        existingCard.Блюдо_id = technologyCard.Блюдо_id;
                        existingCard.Выход = technologyCard.Выход;
                        existingCard.Технология_приготовления = technologyCard.Технология_приготовления;

                        if (technologyCard.Статус == "Утверждена" && existingCard.Статус != "Утверждена")
                        {
                            existingCard.Статус = technologyCard.Статус;
                            existingCard.Кто_утвердил_id = ThisUser.CurrentUser?.Id;
                            existingCard.Дата_утверждения = DateTime.Now;
                        }
                        else if (existingCard.Статус != "Утверждена")
                        {
                            existingCard.Статус = technologyCard.Статус;
                        }

                        db.SaveChanges();

                        // Обновляем стандартный выход в блюде
                        var dish = db.Блюда.Find(technologyCard.Блюдо_id);
                        if (dish != null)
                        {
                            dish.Выход_стандартный = technologyCard.Выход;
                            db.SaveChanges();
                        }

                        return true;
                    }
                    return false;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при обновлении технологической карты: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        public bool DeleteTechnologyCard(int cardId)
        {
            try
            {
                using (var db = new MenuStolovayaDBEntities())
                {
                    var card = db.Технологические_карты.Find(cardId);
                    if (card != null)
                    {
                        int dishId = card.Блюдо_id;

                        bool usedInMenus = db.Строки_меню.Any(sm => sm.Блюдо_id == dishId);
                        bool hasCalcCards = db.Калькуляционные_карточки
                            .Any(cc => cc.Технологическая_карта_id == cardId && cc.Статус == "Утверждена");

                        if (usedInMenus)
                        {
                            MessageBox.Show("Технологическая карта связана с блюдом, которое используется в меню.", "Ошибка",
                                MessageBoxButton.OK, MessageBoxImage.Error);
                            return false;
                        }

                        if (hasCalcCards)
                        {
                            MessageBox.Show("Технологическая карта связана с утвержденной калькуляционной карточкой.", "Ошибка",
                                MessageBoxButton.OK, MessageBoxImage.Error);
                            return false;
                        }

                        // Удаляем рецептуры
                        var recipes = db.Рецептуры.Where(r => r.Технологическая_карта_id == cardId).ToList();
                        foreach (var recipe in recipes)
                        {
                            db.Рецептуры.Remove(recipe);
                        }

                        // Удаляем калькуляционные карточки
                        var calcCards = db.Калькуляционные_карточки
                            .Where(cc => cc.Технологическая_карта_id == cardId)
                            .ToList();

                        foreach (var calcCard in calcCards)
                        {
                            var calcLines = db.Строки_калькуляции
                                .Where(sc => sc.Калькуляционная_карточка_id == calcCard.id)
                                .ToList();

                            foreach (var line in calcLines)
                            {
                                db.Строки_калькуляции.Remove(line);
                            }

                            db.Калькуляционные_карточки.Remove(calcCard);
                        }

                        db.Технологические_карты.Remove(card);
                        db.SaveChanges();

                        // Обновляем калорийность блюда - ИСПРАВЛЕНО: используем UpdateDishCalculations
                        CalorieCalculator.UpdateDishCalculations(cardId);

                        MessageBox.Show("Технологическая карта успешно удалена", "Успех",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                        return true;
                    }

                    MessageBox.Show("Технологическая карта не найдена", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return false;
                }
            }
            catch (System.Data.Entity.Infrastructure.DbUpdateException dbEx)
            {
                MessageBox.Show($"Ошибка при удалении: {dbEx.InnerException?.Message} \n Удалите технологическую карту посредством удаления блюда, связанного с ней.", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при удалении технологической карты: {ex.Message}\n Удалите технологическую карту посредством удаления блюда, связанного с ней.", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        private void UpdateDishCalories(int dishId)
        {
            try
            {
                using (var db = new MenuStolovayaDBEntities())
                {
                    var dish = db.Блюда.Find(dishId);
                    if (dish != null)
                    {
                        // ИСПРАВЛЕНО: используем новый метод CalculateDishCaloriesPer100g
                        dish.Калорийность_расчетная = CalorieCalculator.CalculateDishCaloriesPer100g(dishId);
                        db.SaveChanges();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка при обновлении калорийности блюда {dishId}: {ex.Message}");
            }
        }

        private string GenerateCardNumber()
        {
            return $"ТК-{DateTime.Now:yyyyMMdd}-{new Random().Next(1000, 9999)}";
        }
    }

    public class TechnologyCardDisplay
    {
        public int Id { get; set; }
        public string Номер { get; set; }
        public string Блюдо { get; set; }
        public int Блюдо_id { get; set; }
        public decimal Выход { get; set; }
        public string Выход_отформатированный { get; set; }
        public string Статус { get; set; }
        public DateTime Дата_создания { get; set; }
        public int Количество_ингредиентов { get; set; }
        public string Кто_утвердил { get; set; }
    }
}