using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using MenuStolovaya.Models;

namespace MenuStolovaya.Services
{
    public class MenuService
    {
        public List<DailyMenuDisplay> GetDailyMenus(string filter = "")
        {
            try
            {
                using (var db = new MenuStolovayaDBEntities())
                {
                    var query = db.Меню_на_день
                        .Include("Пользователи")
                        .AsQueryable();

                    if (!string.IsNullOrWhiteSpace(filter))
                    {
                        // Попробуем распарсить фильтр как дату
                        DateTime? filterDate = null;
                        try
                        {
                            if (DateTime.TryParse(filter, out var parsedDate))
                            {
                                filterDate = parsedDate.Date;
                            }
                        }
                        catch { }

                        query = query.Where(m =>
                            (filterDate.HasValue && m.Дата == filterDate.Value) ||
                            (m.Пользователи != null &&
                             (m.Пользователи.Фамилия.Contains(filter) ||
                              m.Пользователи.Имя.Contains(filter))));
                    }

                    // Сначала получаем данные из базы
                    var menus = query.ToList();

                    var result = new List<DailyMenuDisplay>();

                    foreach (var m in menus)
                    {
                        string ответственный = "Неизвестно";
                        if (m.Пользователи != null)
                        {
                            ответственный = $"{m.Пользователи.Фамилия} {m.Пользователи.Имя}";
                        }

                        int количествоБлюд = db.Строки_меню.Count(sm => sm.Меню_id == m.id);
                        int всего_порций = db.Строки_меню
                            .Where(sm => sm.Меню_id == m.id)
                            .Select(sm => sm.Количество_порций)
                            .ToList()
                            .Sum(portions => portions ?? 0);

                        result.Add(new DailyMenuDisplay
                        {
                            Id = m.id,
                            Дата = m.Дата,
                            Ответственный = ответственный,
                            Ответственный_id = m.Ответственный_id,
                            Дата_составления = m.Дата_составления ?? DateTime.Now,
                            Калорийность_общая = m.Калорийность_общая,
                            Статус = m.Статус,
                            Количество_блюд = количествоБлюд,
                            Всего_порций = всего_порций
                        });
                    }

                    return result.OrderByDescending(m => m.Дата).ToList();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке меню: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return new List<DailyMenuDisplay>();
            }
        }

        public List<MenuItemDisplay> GetMenuItems(int menuId)
        {
            try
            {
                using (var db = new MenuStolovayaDBEntities())
                {
                    // Сначала получаем данные из базы с безопасным преобразованием
                    var menuItemsData = db.Строки_меню
                        .Where(sm => sm.Меню_id == menuId)
                        .Join(db.Блюда,
                            sm => sm.Блюдо_id,
                            b => b.id,
                            (sm, b) => new
                            {
                                Id = sm.id,
                                Блюдо_id = b.id,
                                Блюдо = b.Наименование,
                                Вид_блюда = b.Виды_блюд != null ? b.Виды_блюд.Наименование : null,
                                Количество_порций = sm.Количество_порций,
                                Выход_на_порцию = sm.Выход_на_порцию,
                                Время_подачи = sm.Время_подачи,
                                Порядок_подачи = sm.Порядок_подачи
                            })
                        .ToList(); // Выполняем запрос здесь

                    // Теперь безопасно преобразуем на стороне клиента
                    var result = new List<MenuItemDisplay>();

                    foreach (var item in menuItemsData)
                    {
                        int количество_порций = 0;
                        if (item.Количество_порций.HasValue)
                        {
                            количество_порций = item.Количество_порций.Value;
                        }

                        int порядок_подачи = 0;
                        if (item.Порядок_подачи.HasValue)
                        {
                            порядок_подачи = item.Порядок_подачи.Value;
                        }

                        result.Add(new MenuItemDisplay
                        {
                            Id = item.Id,
                            Блюдо_id = item.Блюдо_id,
                            Блюдо = item.Блюдо,
                            Вид_блюда = item.Вид_блюда ?? "Не указано",
                            Количество_порций = количество_порций,
                            Выход_на_порцию = item.Выход_на_порцию,
                            Время_подачи = item.Время_подачи,
                            Порядок_подачи = порядок_подачи
                        });
                    }

                    return result.OrderBy(m => m.Порядок_подачи).ToList();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке строк меню: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return new List<MenuItemDisplay>();
            }
        }

        public bool AddDailyMenu(DailyMenuModel menu)
        {
            try
            {
                using (var db = new MenuStolovayaDBEntities())
                {
                    // Проверяем, не существует ли уже меню на эту дату
                    var existingMenu = db.Меню_на_день
                        .FirstOrDefault(m => m.Дата == menu.Дата.Date && m.Статус != "Черновик");

                    if (existingMenu != null && menu.Статус != "Черновик")
                    {
                        MessageBox.Show($"Меню на дату {menu.Дата:dd.MM.yyyy} уже существует", "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                        return false;
                    }

                    var newMenu = new Меню_на_день
                    {
                        Дата = menu.Дата,
                        Ответственный_id = menu.Ответственный_id,
                        Дата_составления = DateTime.Now,
                        Статус = "Черновик"
                    };

                    db.Меню_на_день.Add(newMenu);
                    db.SaveChanges();
                    return true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при добавлении меню: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        public bool UpdateDailyMenu(DailyMenuModel menu)
        {
            try
            {
                using (var db = new MenuStolovayaDBEntities())
                {
                    var existingMenu = db.Меню_на_день.Find(menu.Id);
                    if (existingMenu != null)
                    {
                        // Проверяем дату только если она изменилась
                        if (existingMenu.Дата != menu.Дата.Date && menu.Статус != "Черновик")
                        {
                            var conflictMenu = db.Меню_на_день
                                .FirstOrDefault(m => m.Дата == menu.Дата.Date &&
                                                   m.id != menu.Id &&
                                                   m.Статус != "Черновик");

                            if (conflictMenu != null)
                            {
                                MessageBox.Show($"Меню на дату {menu.Дата:dd.MM.yyyy} уже существует", "Ошибка",
                                    MessageBoxButton.OK, MessageBoxImage.Warning);
                                return false;
                            }
                        }

                        existingMenu.Дата = menu.Дата;
                        existingMenu.Статус = menu.Статус;

                        db.SaveChanges();
                        return true;
                    }
                    return false;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при обновлении меню: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        public bool DeleteDailyMenu(int menuId)
        {
            try
            {
                using (var db = new MenuStolovayaDBEntities())
                {
                    var menu = db.Меню_на_день.Find(menuId);
                    if (menu != null)
                    {
                        db.Меню_на_день.Remove(menu);
                        db.SaveChanges();
                        return true;
                    }
                    return false;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при удалении меню: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        public bool AddMenuItem(MenuItemModel menuItem)
        {
            try
            {
                using (var db = new MenuStolovayaDBEntities())
                {
                    // Проверяем, не добавлено ли уже это блюдо в меню
                    var existing = db.Строки_меню
                        .FirstOrDefault(sm => sm.Меню_id == menuItem.Меню_id &&
                                            sm.Блюдо_id == menuItem.Блюдо_id);

                    if (existing != null)
                    {
                        MessageBox.Show("Это блюдо уже добавлено в меню", "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                        return false;
                    }

                    // Получаем следующий порядковый номер
                    var maxOrder = db.Строки_меню
                        .Where(sm => sm.Меню_id == menuItem.Меню_id)
                        .Select(sm => sm.Порядок_подачи)
                        .ToList() // Выполняем на стороне клиента
                        .Max() ?? 0;

                    var newItem = new Строки_меню
                    {
                        Меню_id = menuItem.Меню_id,
                        Блюдо_id = menuItem.Блюдо_id,
                        Количество_порций = menuItem.Количество_порций,
                        Выход_на_порцию = menuItem.Выход_на_порцию,
                        Время_подачи = menuItem.Время_подачи,
                        Порядок_подачи = maxOrder + 1
                    };

                    db.Строки_меню.Add(newItem);
                    db.SaveChanges();
                    return true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при добавлении блюда в меню: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        public bool UpdateMenuItem(MenuItemModel menuItem)
        {
            try
            {
                using (var db = new MenuStolovayaDBEntities())
                {
                    var existingItem = db.Строки_меню.Find(menuItem.Id);
                    if (existingItem != null)
                    {
                        existingItem.Количество_порций = menuItem.Количество_порций;
                        existingItem.Выход_на_порцию = menuItem.Выход_на_порцию;
                        existingItem.Время_подачи = menuItem.Время_подачи;
                        existingItem.Порядок_подачи = menuItem.Порядок_подачи;
                        db.SaveChanges();

                        // Пересчитываем общую калорийность меню
                        UpdateMenuCalories(menuItem.Меню_id);

                        return true;
                    }
                    return false;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при обновлении строки меню: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        public bool DeleteMenuItem(int menuItemId)
        {
            try
            {
                using (var db = new MenuStolovayaDBEntities())
                {
                    var menuItem = db.Строки_меню.Find(menuItemId);
                    if (menuItem != null)
                    {
                        // Сохраняем ID меню перед удалением
                        int menuId = menuItem.Меню_id;

                        db.Строки_меню.Remove(menuItem);
                        db.SaveChanges();

                        // Обновляем порядковые номера
                        UpdateMenuOrder(menuId);

                        // Пересчитываем общую калорийность меню
                        UpdateMenuCalories(menuId);

                        return true;
                    }
                    return false;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при удалении из меню: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }
        public bool UpdateMenuCalories(int menuId)
        {
            try
            {
                using (var db = new MenuStolovayaDBEntities())
                {
                    // Рассчитываем общую калорийность меню
                    decimal totalCalories = db.Строки_меню
                        .Where(sm => sm.Меню_id == menuId)
                        .Join(db.Блюда,
                            sm => sm.Блюдо_id,
                            b => b.id,
                            (sm, b) => new
                            {
                                Порций = sm.Количество_порций ?? 1,
                                ВыходПорции = sm.Выход_на_порцию ?? b.Выход_стандартный ?? 100,
                                Калорийность = b.Калорийность_расчетная ?? 0
                            })
                        .ToList()
                        .Sum(item => (item.Калорийность / 100m * item.ВыходПорции) * item.Порций);

                    // Обновляем в базе
                    var menu = db.Меню_на_день.Find(menuId);
                    if (menu != null)
                    {
                        menu.Калорийность_общая = totalCalories;
                        db.SaveChanges();
                        return true;
                    }
                    return false;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при расчете калорийности меню: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }


        private void UpdateMenuOrder(int menuId)
        {
            try
            {
                using (var db = new MenuStolovayaDBEntities())
                {
                    var items = db.Строки_меню
                        .Where(sm => sm.Меню_id == menuId)
                        .ToList() // Выполняем на стороне клиента
                        .OrderBy(sm => sm.Порядок_подачи ?? 0)
                        .ToList();

                    for (int i = 0; i < items.Count; i++)
                    {
                        items[i].Порядок_подачи = i + 1;
                    }

                    db.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при обновлении порядка блюд: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }



    public class DailyMenuDisplay
    {
        public int Id { get; set; }
        public DateTime Дата { get; set; }
        public string Ответственный { get; set; }
        public int Ответственный_id { get; set; }
        public DateTime Дата_составления { get; set; }
        public decimal? Калорийность_общая { get; set; }
        public string Статус { get; set; }
        public int Количество_блюд { get; set; }
        public int Всего_порций { get; set; }
    }

    public class MenuItemDisplay
    {
        public int Id { get; set; }
        public int Блюдо_id { get; set; }
        public string Блюдо { get; set; }
        public string Вид_блюда { get; set; }
        public int Количество_порций { get; set; }
        public decimal? Выход_на_порцию { get; set; }
        public string Время_подачи { get; set; }
        public int Порядок_подачи { get; set; }
    }
}