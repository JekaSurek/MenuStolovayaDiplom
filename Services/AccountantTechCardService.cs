using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using MenuStolovaya.Models;

namespace MenuStolovaya.Services
{
    public class AccountantTechCardService
    {
        public List<AccountantTechCardDisplay> GetTechCards(string filter = "")
        {
            try
            {
                using (var db = new MenuStolovayaDBEntities())
                {
                    // Сначала получаем данные из представления
                    var query = db.vw_Технологические_карты_полные.AsQueryable();

                    if (!string.IsNullOrWhiteSpace(filter))
                    {
                        query = query.Where(tc =>
                            tc.Номер.Contains(filter) ||
                            tc.Блюдо.Contains(filter));
                    }

                    // Выполняем запрос и получаем данные в память
                    var viewData = query.ToList();

                    // Теперь преобразуем данные, используя локальные методы
                    var result = new List<AccountantTechCardDisplay>();

                    foreach (var tc in viewData)
                    {
                        // Получаем ID технологической карты по номеру
                        int techCardId = GetTechCardIdByNumber(tc.Номер);

                        result.Add(new AccountantTechCardDisplay
                        {
                            Id = techCardId,
                            Номер = tc.Номер,
                            Блюдо = tc.Блюдо,
                            Выход = tc.Выход_порции_г,
                            Статус = tc.Статус,
                            Дата_создания = tc.Дата_создания ?? DateTime.Now,
                            Количество_ингредиентов = tc.Количество_ингредиентов ?? 0,
                            Кто_утвердил = tc.Кто_утвердил,
                            Дата_утверждения = tc.Дата_утверждения
                        });
                    }

                    return result;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке технологических карт: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return new List<AccountantTechCardDisplay>();
            }
        }

        private int GetTechCardIdByNumber(string number)
        {
            try
            {
                using (var db = new MenuStolovayaDBEntities())
                {
                    var card = db.Технологические_карты
                        .FirstOrDefault(tc => tc.Номер == number);
                    return card?.id ?? 0;
                }
            }
            catch
            {
                return 0;
            }
        }

        public bool ApproveTechCard(int techCardId)
        {
            try
            {
                using (var db = new MenuStolovayaDBEntities())
                {
                    var techCard = db.Технологические_карты.Find(techCardId);
                    if (techCard == null)
                    {
                        MessageBox.Show("Технологическая карта не найдена",
                            "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        return false;
                    }

                    if (techCard.Статус == "Утверждена")
                    {
                        MessageBox.Show("Технологическая карта уже утверждена",
                            "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                        return false;
                    }

                    techCard.Статус = "Утверждена";
                    techCard.Кто_утвердил_id = ThisUser.CurrentUser?.Id;
                    techCard.Дата_утверждения = DateTime.Now;

                    db.SaveChanges();
                    return true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при утверждении технологической карты: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        public AccountantTechCardDetails GetTechCardDetails(int techCardId)
        {
            try
            {
                using (var db = new MenuStolovayaDBEntities())
                {
                    var techCard = db.Технологические_карты
                        .Include("Блюда")
                        .Include("Блюда.Виды_блюд")
                        .FirstOrDefault(tc => tc.id == techCardId);

                    if (techCard == null)
                        return null;

                    var recipes = db.Рецептуры
                        .Where(r => r.Технологическая_карта_id == techCardId)
                        .Join(db.Продукты,
                            r => r.Продукт_id,
                            p => p.id,
                            (r, p) => new AccountantRecipeDetail
                            {
                                Продукт = p.Наименование,
                                Единица_измерения = p.Единица_измерения,
                                Количество_брутто = r.Количество_брутто,
                                Количество_нетто = r.Количество_нетто ?? r.Количество_брутто,
                                Порядок_закладки = r.Порядок_закладки ?? 0
                            })
                        .OrderBy(r => r.Порядок_закладки)
                        .ToList();

                    return new AccountantTechCardDetails
                    {
                        Номер = techCard.Номер,
                        Блюдо = techCard.Блюда?.Наименование ?? "Неизвестно",
                        Вид_блюда = techCard.Блюда?.Виды_блюд?.Наименование ?? "Не указано",
                        Выход = techCard.Выход,
                        Технология_приготовления = techCard.Технология_приготовления,
                        Статус = techCard.Статус,
                        Дата_создания = techCard.Дата_создания ?? DateTime.Now,
                        Рецептура = recipes
                    };
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при получении деталей технологической карты: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }
        }
    }

    public class AccountantTechCardDisplay
    {
        public int Id { get; set; }
        public string Номер { get; set; }
        public string Блюдо { get; set; }
        public decimal Выход { get; set; }
        public string Статус { get; set; }
        public DateTime Дата_создания { get; set; }
        public int Количество_ингредиентов { get; set; }
        public string Кто_утвердил { get; set; }
        public DateTime? Дата_утверждения { get; set; }
    }

    public class AccountantTechCardDetails
    {
        public string Номер { get; set; }
        public string Блюдо { get; set; }
        public string Вид_блюда { get; set; }
        public decimal Выход { get; set; }
        public string Технология_приготовления { get; set; }
        public string Статус { get; set; }
        public DateTime Дата_создания { get; set; }
        public List<AccountantRecipeDetail> Рецептура { get; set; }
    }

    public class AccountantRecipeDetail
    {
        public string Продукт { get; set; }
        public string Единица_измерения { get; set; }
        public decimal Количество_брутто { get; set; }
        public decimal Количество_нетто { get; set; }
        public int Порядок_закладки { get; set; }
    }
}