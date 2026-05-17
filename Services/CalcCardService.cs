using MenuStolovaya.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Windows;

namespace MenuStolovaya.Services
{
    public class CalcCardService
    {
        public List<CalcCardDisplay> GetCalcCards(string filter = "")
        {
            try
            {
                using (var db = new MenuStolovayaDBEntities())
                {
                    // Используем представление из базы данных
                    var query = db.vw_Калькуляционные_карточки_полные.AsQueryable();

                    if (!string.IsNullOrWhiteSpace(filter))
                    {
                        query = query.Where(c =>
                            c.Номер.Contains(filter) ||
                            c.Блюдо.Contains(filter) ||
                            c.Тех_карта.Contains(filter));
                    }

                    // Сначала получаем данные из базы
                    var cardsFromDb = query.ToList();
                    var result = new List<CalcCardDisplay>();

                    // Затем обрабатываем в памяти
                    foreach (var c in cardsFromDb)
                    {
                        // Проверяем на null для nullable полей
                        decimal себестоимость = c.Себестоимость.HasValue ? c.Себестоимость.Value : 0;
                        decimal цена = c.Цена_реализации.HasValue ? c.Цена_реализации.Value : 0;
                        decimal наценка = c.Процент_наценки.HasValue ? c.Процент_наценки.Value : 0;

                        // Получаем ID карточки по номеру
                        int calcCardId = GetCalcCardIdByNumber(c.Номер);

                        result.Add(new CalcCardDisplay
                        {
                            Id = calcCardId,
                            Номер = c.Номер,
                            Блюдо = c.Блюдо,
                            Выход_порции_г = c.Выход_порции_г,
                            Себестоимость = себестоимость,
                            Цена_реализации = цена,
                            Фудкост_процент = c.Фудкост_процент,
                            Процент_наценки = наценка,
                            Маржинальность = c.Маржинальность,
                            Статус = c.Статус,
                            Дата_составления = c.Дата_составления ?? DateTime.Now,
                            Кто_утвердил = c.Кто_утвердил,
                            Тех_карта = c.Тех_карта
                        });
                    }

                    return result;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке калькуляционных карточек: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return new List<CalcCardDisplay>();
            }
        }

        private int GetCalcCardIdByNumber(string number)
        {
            try
            {
                using (var db = new MenuStolovayaDBEntities())
                {
                    var card = db.Калькуляционные_карточки
                        .FirstOrDefault(cc => cc.Номер == number);
                    return card?.id ?? 0;
                }
            }
            catch
            {
                return 0;
            }
        }

        public bool ApproveCalcCard(int calcCardId)
        {
            try
            {
                using (var db = new MenuStolovayaDBEntities())
                {
                    var card = db.Калькуляционные_карточки.Find(calcCardId);
                    if (card == null)
                    {
                        MessageBox.Show("Калькуляционная карточка не найдена",
                            "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        return false;
                    }

                    if (card.Статус == "Утверждена")
                    {
                        MessageBox.Show("Калькуляционная карточка уже утверждена",
                            "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                        return false;
                    }

                    // Проверяем, существует ли утвержденная технологическая карта
                    var techCard = db.Технологические_карты.Find(card.Технологическая_карта_id);
                    if (techCard == null || techCard.Статус != "Утверждена")
                    {
                        MessageBox.Show("Нельзя утвердить калькуляционную карточку без утвержденной технологической карты",
                            "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        return false;
                    }

                    card.Статус = "Утверждена";
                    card.Кто_утвердил_id = ThisUser.CurrentUser?.Id;
                    card.Дата_утверждения = DateTime.Now;

                    db.SaveChanges();
                    return true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при утверждении калькуляционной карточки: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        public bool CreateCalcCardFromTechCard(int techCardId)
        {
            try
            {
                using (var db = new MenuStolovayaDBEntities())
                {
                    var techCard = db.Технологические_карты
                        .Include("Блюда")
                        .FirstOrDefault(tc => tc.id == techCardId);

                    if (techCard == null || techCard.Статус != "Утверждена")
                    {
                        MessageBox.Show("Технологическая карта не найдена или не утверждена",
                            "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        return false;
                    }

                    // Проверяем, нет ли уже калькуляционной карточки для этой технологической карты
                    var existingCalcCard = db.Калькуляционные_карточки
                        .FirstOrDefault(cc => cc.Технологическая_карта_id == techCardId);

                    if (existingCalcCard != null)
                    {
                        MessageBox.Show("Для этой технологической карты уже существует калькуляционная карточка",
                            "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        return false;
                    }

                    // Генерируем номер калькуляционной карточки
                    string cardNumber = $"КК-{DateTime.Now:yyyyMMdd}-{new Random().Next(1000, 9999)}";

                    var newCalcCard = new Калькуляционные_карточки
                    {
                        Номер = cardNumber,
                        Технологическая_карта_id = techCardId,
                        Дата_составления = DateTime.Now,
                        Статус = "Черновик",
                        Процент_наценки = 150 // Значение по умолчанию
                    };

                    db.Калькуляционные_карточки.Add(newCalcCard);
                    db.SaveChanges();

                    // Автоматически создаем строки калькуляции на основе рецептуры
                    CreateCalcLinesFromRecipes(newCalcCard.id, techCardId);

                    return true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при создании калькуляционной карточки: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        private bool CreateCalcLinesFromRecipes(int calcCardId, int techCardId)
        {
            try
            {
                using (var db = new MenuStolovayaDBEntities())
                {
                    var recipes = db.Рецептуры
                        .Where(r => r.Технологическая_карта_id == techCardId)
                        .Include("Продукты")
                        .ToList();

                    if (!recipes.Any())
                    {
                        MessageBox.Show("В технологической карте нет ингредиентов",
                            "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        return false;
                    }

                    foreach (var recipe in recipes)
                    {
                        var product = recipe.Продукты;
                        if (product == null)
                        {
                            continue;
                        }

                        // Получаем значения, проверяя на null
                        decimal количествоНетто = recipe.Количество_нетто ?? recipe.Количество_брутто;
                        decimal ценаПродукта = product.Цена ?? 0;

                        var calcLine = new Строки_калькуляции
                        {
                            Калькуляционная_карточка_id = calcCardId,
                            Продукт_id = product.id,
                            Норма_расхода = количествоНетто,
                            Цена_за_единицу = ценаПродукта,
                            Сумма = количествоНетто * ценаПродукта
                        };

                        db.Строки_калькуляции.Add(calcLine);
                    }

                    db.SaveChanges();
                    return true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при создании строк калькуляции: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        private void UpdateCalcCardCost(int calcCardId)
        {
            try
            {
                using (var db = new MenuStolovayaDBEntities())
                {
                    var calcCard = db.Калькуляционные_карточки.Find(calcCardId);
                    if (calcCard == null) return;

                    var totalCost = db.Строки_калькуляции
                        .Where(cl => cl.Калькуляционная_карточка_id == calcCardId)
                        .Sum(cl => cl.Сумма);

                    calcCard.Себестоимость = totalCost;

                    // Рассчитываем цену реализации с учетом наценки
                    if (calcCard.Процент_наценки.HasValue && calcCard.Процент_наценки.Value > 0)
                    {
                        calcCard.Цена_реализации = totalCost * (1 + calcCard.Процент_наценки.Value / 100);
                    }
                    else
                    {
                        calcCard.Цена_реализации = totalCost * 2.5m; // Наценка 150% по умолчанию
                    }

                    db.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при расчете себестоимости: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public List<CalcLineDisplay> GetCalcLines(int calcCardId)
        {
            try
            {
                using (var db = new MenuStolovayaDBEntities())
                {
                    var lines = db.Строки_калькуляции
                        .Where(cl => cl.Калькуляционная_карточка_id == calcCardId)
                        .Join(db.Продукты,
                            cl => cl.Продукт_id,
                            p => p.id,
                            (cl, p) => new CalcLineDisplay
                            {
                                Id = cl.id,
                                Продукт = p.Наименование,
                                Единица_измерения = p.Единица_измерения,
                                Норма_расхода = cl.Норма_расхода,
                                Цена_за_единицу = cl.Цена_за_единицу,
                                Сумма = cl.Сумма
                            })
                        .ToList();

                    return lines;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке строк калькуляции: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return new List<CalcLineDisplay>();
            }
        }

        public CalcCardInfo GetCalcCardInfo(int calcCardId)
        {
            try
            {
                using (var db = new MenuStolovayaDBEntities())
                {
                    var calcCard = db.Калькуляционные_карточки
                        .Include("Технологические_карты")
                        .Include("Технологические_карты.Блюда")
                        .FirstOrDefault(cc => cc.id == calcCardId);

                    if (calcCard == null) return null;

                    return new CalcCardInfo
                    {
                        Номер = calcCard.Номер,
                        Блюдо = calcCard.Технологические_карты?.Блюда?.Наименование ?? "Неизвестно",
                        Выход = calcCard.Технологические_карты?.Выход ?? 0,
                        Себестоимость = calcCard.Себестоимость ?? 0,
                        Цена_реализации = calcCard.Цена_реализации ?? 0,
                        Процент_наценки = calcCard.Процент_наценки ?? 0
                    };
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при получении информации о калькуляционной карточке: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }
        }
        public bool DeleteCalcCard(int calcCardId)
        {
            try
            {
                using (var db = new MenuStolovayaDBEntities())
                {
                    var card = db.Калькуляционные_карточки
                        .Include("Строки_калькуляции")
                        .FirstOrDefault(cc => cc.id == calcCardId);

                    if (card == null)
                    {
                        MessageBox.Show("Калькуляционная карточка не найдена",
                            "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        return false;
                    }

                    if (card.Статус == "Утверждена")
                    {
                        MessageBox.Show("Нельзя удалить утвержденную калькуляционную карточку.\n" +
                            "Сначала отправьте её на пересмотр.",
                            "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        return false;
                    }

                    // Удаляем сначала строки калькуляции (альтернативный способ)
                    var calcLines = db.Строки_калькуляции
                        .Where(cl => cl.Калькуляционная_карточка_id == calcCardId)
                        .ToList();

                    foreach (var line in calcLines)
                    {
                        db.Строки_калькуляции.Remove(line);
                    }

                    // Затем удаляем саму карточку
                    db.Калькуляционные_карточки.Remove(card);

                    db.SaveChanges();
                    return true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при удалении калькуляционной карточки: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        public bool ReturnToReview(int calcCardId)
        {
            try
            {
                using (var db = new MenuStolovayaDBEntities())
                {
                    var card = db.Калькуляционные_карточки.Find(calcCardId);
                    if (card == null)
                    {
                        MessageBox.Show("Калькуляционная карточка не найдена",
                            "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        return false;
                    }

                    if (card.Статус != "Утверждена")
                    {
                        MessageBox.Show("Можно отправить на пересмотр только утвержденные карточки",
                            "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        return false;
                    }

                    card.Статус = "На пересмотре";
                    card.Кто_утвердил_id = null;
                    card.Дата_утверждения = null;

                    db.SaveChanges();
                    return true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при отправке на пересмотр: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        public bool ReturnToDraft(int calcCardId)
        {
            try
            {
                using (var db = new MenuStolovayaDBEntities())
                {
                    var card = db.Калькуляционные_карточки.Find(calcCardId);
                    if (card == null)
                    {
                        MessageBox.Show("Калькуляционная карточка не найдена",
                            "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        return false;
                    }

                    if (card.Статус != "На пересмотре")
                    {
                        MessageBox.Show("Можно вернуть в черновик только карточки на пересмотре",
                            "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        return false;
                    }

                    card.Статус = "Черновик";

                    db.SaveChanges();
                    return true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при возврате в черновик: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        // Обновите метод CreateCalcCardFromTechCard для корректного создания карточек:
        public bool CreateCalcCardFromTechCard(int techCardId, decimal markup = 150)
        {
            try
            {
                using (var db = new MenuStolovayaDBEntities())
                {
                    var techCard = db.Технологические_карты
                        .Include("Блюда")
                        .FirstOrDefault(tc => tc.id == techCardId);

                    if (techCard == null || techCard.Статус != "Утверждена")
                    {
                        MessageBox.Show("Технологическая карта не найдена или не утверждена",
                            "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        return false;
                    }

                    // Проверяем, нет ли уже калькуляционной карточки для этой технологической карты
                    var existingCalcCard = db.Калькуляционные_карточки
                        .FirstOrDefault(cc => cc.Технологическая_карта_id == techCardId);

                    if (existingCalcCard != null)
                    {
                        MessageBox.Show("Для этой технологической карты уже существует калькуляционная карточка",
                            "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        return false;
                    }

                    // Генерируем номер калькуляционной карточки
                    string cardNumber = $"КК-{DateTime.Now:yyyyMMdd}-{new Random().Next(1000, 9999)}";

                    var newCalcCard = new Калькуляционные_карточки
                    {
                        Номер = cardNumber,
                        Технологическая_карта_id = techCardId,
                        Дата_составления = DateTime.Now,
                        Статус = "Черновик",
                        Процент_наценки = markup
                    };

                    db.Калькуляционные_карточки.Add(newCalcCard);
                    db.SaveChanges();

                    // Автоматически создаем строки калькуляции на основе рецептуры
                    bool linesCreated = CreateCalcLinesFromRecipes(newCalcCard.id, techCardId);

                    if (!linesCreated)
                    {
                        // Если не удалось создать строки, удаляем карточку
                        db.Калькуляционные_карточки.Remove(newCalcCard);
                        db.SaveChanges();
                        return false;
                    }

                    // Пересчитываем себестоимость и цену реализации
                    UpdateCalcCardCost(newCalcCard.id);

                    return true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при создании калькуляционной карточки: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        public bool CanDeleteCalcCard(int calcCardId)
        {
            try
            {
                using (var db = new MenuStolovayaDBEntities())
                {
                    var card = db.Калькуляционные_карточки.Find(calcCardId);
                    return card != null && card.Статус != "Утверждена";
                }
            }
            catch
            {
                return false;
            }
        }


    }

    public class CalcLineDisplay
    {
        public int Id { get; set; }
        public string Продукт { get; set; }
        public string Единица_измерения { get; set; }
        public decimal Норма_расхода { get; set; }
        public decimal Цена_за_единицу { get; set; }
        public decimal Сумма { get; set; }
    }

    public class CalcCardInfo
    {
        public string Номер { get; set; }
        public string Блюдо { get; set; }
        public decimal Выход { get; set; }
        public decimal Себестоимость { get; set; }
        public decimal Цена_реализации { get; set; }
        public decimal Процент_наценки { get; set; }
    }

    public class CalcCardDisplay
    {
        public int Id { get; set; }
        public string Номер { get; set; }
        public string Блюдо { get; set; }
        public decimal Выход_порции_г { get; set; }
        public decimal Себестоимость { get; set; }
        public decimal Цена_реализации { get; set; }
        public decimal? Фудкост_процент { get; set; }
        public decimal Процент_наценки { get; set; }
        public string Маржинальность { get; set; }
        public string Статус { get; set; }
        public DateTime Дата_составления { get; set; }
        public string Кто_утвердил { get; set; }
        public string Тех_карта { get; set; }
    }
}