using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Text.RegularExpressions;
using MenuStolovaya.Models;
using MenuStolovaya.Services;

namespace MenuStolovaya.Views
{
    public partial class MenuEditorWindow : Window
    {
        private int? _menuId;
        private bool _isNewMenu;
        private MenuService _menuService;
        private MenuPrinterService _menuPrinterService;
        private List<MenuItemWithDetails> _menuItems;
        private decimal _totalCalories;

        public MenuEditorWindow(int? menuId = null)
        {
            InitializeComponent();
            _menuId = menuId;
            _isNewMenu = !menuId.HasValue;
            _menuService = new MenuService();
            _menuPrinterService = new MenuPrinterService();

            LoadData();
        }

        private void LoadData()
        {
            LoadDishes();

            if (!_isNewMenu && _menuId.HasValue)
            {
                LoadMenuData();
                LoadMenuItems();
            }
            else
            {
                // Новая карта
                TitleText.Text = "Новое меню";
                SubtitleText.Text = "Выберите дату и добавьте блюда";
                MenuDatePicker.SelectedDate = DateTime.Today;
                CreationDateText.Text = DateTime.Now.ToString("dd.MM.yyyy HH:mm");

                using (var db = new MenuStolovayaDBEntities())
                {
                    var user = db.Пользователи.Find(ThisUser.CurrentUser.Id);
                    ResponsibleText.Text = user != null ? $"{user.Фамилия} {user.Имя}" : ThisUser.CurrentUser.FullName;
                }

                StatusComboBox.SelectedIndex = 0;
                _menuItems = new List<MenuItemWithDetails>();
                UpdateMenuItemsDisplay();
                UpdateStats();
            }
        }

        private void LoadDishes()
        {
            try
            {
                using (var db = new MenuStolovayaDBEntities())
                {
                    var dishesRaw = db.Блюда
                        .Where(d => d.Активно == true)
                        .Select(d => new
                        {
                            d.id,
                            d.Наименование,
                            StandardOutput = d.Выход_стандартный ?? 100,
                            // ИСПРАВЛЕНО: Калорийность_расчетная уже в ккал/100г
                            CaloriesPer100g = d.Калорийность_расчетная ?? 0,
                            DishType = d.Виды_блюд != null ? d.Виды_блюд.Наименование : "Не указано"
                        })
                        .ToList();

                    // ИСПРАВЛЕНО: Убрано деление на 1000
                    var dishes = dishesRaw.Select(d => new
                    {
                        d.id,
                        d.Наименование,
                        DisplayText = $"{d.Наименование} ({d.StandardOutput}г, {d.CaloriesPer100g:F1} ккал/100г)",
                        d.StandardOutput,
                        d.CaloriesPer100g,
                        d.DishType
                    }).ToList();

                    DishComboBox.ItemsSource = dishes;
                    DishComboBox.DisplayMemberPath = "DisplayText";
                    DishComboBox.SelectedValuePath = "id";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки блюд: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadMenuData()
        {
            try
            {
                using (var db = new MenuStolovayaDBEntities())
                {
                    var menu = db.Меню_на_день.Find(_menuId.Value);
                    if (menu == null) return;

                    TitleText.Text = $"Редактирование меню";
                    SubtitleText.Text = $"Меню от {menu.Дата:dd.MM.yyyy}";

                    MenuDatePicker.SelectedDate = menu.Дата;
                    CreationDateText.Text = (menu.Дата_составления ?? DateTime.Now).ToString("dd.MM.yyyy HH:mm");

                    var responsible = db.Пользователи.Find(menu.Ответственный_id);
                    ResponsibleText.Text = responsible != null ? $"{responsible.Фамилия} {responsible.Имя}" : "Неизвестно";

                    StatusComboBox.SelectedItem = StatusComboBox.Items
                        .Cast<ComboBoxItem>()
                        .FirstOrDefault(i => i.Content.ToString() == menu.Статус);

                    _totalCalories = menu.Калорийность_общая ?? 0;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки меню: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadMenuItems()
        {
            if (!_menuId.HasValue) return;

            try
            {
                using (var db = new MenuStolovayaDBEntities())
                {
                    var items = db.Строки_меню
                        .Where(sm => sm.Меню_id == _menuId.Value)
                        .Join(db.Блюда,
                            sm => sm.Блюдо_id,
                            b => b.id,
                            (sm, b) => new MenuItemWithDetails
                            {
                                Id = sm.id,
                                Блюдо_id = b.id,
                                Блюдо = b.Наименование,
                                Вид_блюда = b.Виды_блюд != null ? b.Виды_блюд.Наименование : "Не указано",
                                Количество_порций = sm.Количество_порций ?? 1,
                                Выход_на_порцию = sm.Выход_на_порцию ?? b.Выход_стандартный ?? 100,
                                Время_подачи = sm.Время_подачи,
                                Порядок_подачи = sm.Порядок_подачи ?? 0,
                                Калорийность_брюда = b.Калорийность_расчетная ?? 0
                            })
                        .ToList();

                    foreach (var item in items)
                    {
                        item.Калорийность_на_порцию = (item.Калорийность_брюда / 100m) * item.Выход_на_порцию;
                        item.Калорийность_всего = item.Калорийность_на_порцию * item.Количество_порций;
                    }

                    _menuItems = items.OrderBy(i => i.Порядок_подачи).ToList();
                    UpdateMenuItemsDisplay();
                    UpdateStats();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки блюд меню: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateMenuItemsDisplay()
        {
            if (_menuItems == null || !_menuItems.Any())
            {
                MenuItemsControl.ItemsSource = null;
                return;
            }

            // Группировка по времени подачи
            var groups = _menuItems
                .GroupBy(m => m.Время_подачи)
                .OrderBy(g => GetTimeOrder(g.Key))
                .Select(g => new TimeGroup
                {
                    Name = g.Key,  // ← Исправлено: переименовано с TimeGroup на Name
                    Items = g.OrderBy(i => i.Порядок_подачи).ToList()
                })
                .ToList();

            MenuItemsControl.ItemsSource = groups;
        }

        private int GetTimeOrder(string time)
        {
            switch (time)
            {
                case "Завтрак": return 1;
                case "Обед": return 2;
                case "Ужин": return 3;
                case "Полдник": return 4;
                default: return 5;
            }
        }

        private void UpdateStats()
        {
            if (_menuItems == null)
            {
                TotalDishesText.Text = "0";
                TotalPortionsText.Text = "0";
                TotalCaloriesText.Text = "0 ккал";  // ИСПРАВЛЕНО
                return;
            }

            TotalDishesText.Text = _menuItems.Count.ToString();
            TotalPortionsText.Text = _menuItems.Sum(i => i.Количество_порций).ToString();

            _totalCalories = _menuItems.Sum(i => i.Калорийность_всего);
            TotalCaloriesText.Text = $"{_totalCalories:F0} ккал";  // ИСПРАВЛЕНО
        }

        private void RecalculateCalories()
        {
            _totalCalories = _menuItems.Sum(i => i.Калорийность_всего);
            TotalCaloriesText.Text = $"{_totalCalories:F0} ккал";  // ИСПРАВЛЕНО
        }

        private void DishComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DishComboBox.SelectedItem != null)
            {
                dynamic selected = DishComboBox.SelectedItem;
                decimal standardOutput = selected.StandardOutput;
                OutputTextBox.Text = standardOutput.ToString("F0");
            }
        }

        private void UseStandardOutputButton_Click(object sender, RoutedEventArgs e)
        {
            if (DishComboBox.SelectedItem != null)
            {
                dynamic selected = DishComboBox.SelectedItem;
                OutputTextBox.Text = selected.StandardOutput.ToString("F0");
            }
        }

        private void MenuDatePicker_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            // Проверка на существующее меню
            if (MenuDatePicker.SelectedDate.HasValue && _isNewMenu)
            {
                using (var db = new MenuStolovayaDBEntities())
                {
                    // Исправлено: получаем дату в переменную и используем DbFunctions.TruncateTime или EntityFunctions
                    DateTime selectedDate = MenuDatePicker.SelectedDate.Value.Date;

                    var existing = db.Меню_на_день
                        .Where(m => m.Дата == selectedDate)
                        .FirstOrDefault();

                    if (existing != null)
                    {
                        MessageBox.Show($"Меню на дату {selectedDate:dd.MM.yyyy} уже существует!\n" +
                                       "Будет создана новая версия или выберите другую дату.",
                            "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
            }
        }

        private void AddDishButton_Click(object sender, RoutedEventArgs e)
        {
            if (DishComboBox.SelectedItem == null)
            {
                MessageBox.Show("Выберите блюдо", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!int.TryParse(PortionsTextBox.Text, out int portions) || portions <= 0)
            {
                MessageBox.Show("Введите корректное количество порций", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!decimal.TryParse(OutputTextBox.Text, out decimal output) || output <= 0)
            {
                MessageBox.Show("Введите корректный выход на порцию", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            dynamic selected = DishComboBox.SelectedItem;
            int dishId = selected.id;
            string dishName = selected.Наименование;
            string dishType = selected.DishType;
            decimal caloriesPer100g = selected.CaloriesPer100g;

            // Проверка на дубликат
            var existing = _menuItems.FirstOrDefault(i => i.Блюдо_id == dishId);
            if (existing != null)
            {
                var result = MessageBox.Show($"Блюдо \"{dishName}\" уже добавлено в меню.\n" +
                    "Добавить еще одну порцию?", "Подтверждение",
                    MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    existing.Количество_порций += portions;
                    existing.Калорийность_на_порцию = (caloriesPer100g / 100m) * output;
                    existing.Калорийность_всего = existing.Калорийность_на_порцию * existing.Количество_порций;
                }
                else
                {
                    return;
                }
            }
            else
            {
                var timeItem = TimeComboBox.SelectedItem as ComboBoxItem;
                string time = timeItem?.Content?.ToString() ?? "Обед";

                int maxOrder = _menuItems.Any() ? _menuItems.Max(i => i.Порядок_подачи) + 1 : 1;

                var newItem = new MenuItemWithDetails
                {
                    Блюдо_id = dishId,
                    Блюдо = dishName,
                    Вид_блюда = dishType,
                    Количество_порций = portions,
                    Выход_на_порцию = output,
                    Время_подачи = time,
                    Порядок_подачи = maxOrder,
                    Калорийность_брюда = caloriesPer100g,
                    Калорийность_на_порцию = (caloriesPer100g / 100m) * output,
                    Калорийность_всего = ((caloriesPer100g / 100m) * output) * portions
                };

                _menuItems.Add(newItem);
            }

            UpdateMenuItemsDisplay();
            UpdateStats();

            // Сброс полей
            DishComboBox.SelectedIndex = -1;
            PortionsTextBox.Text = "1";
            OutputTextBox.Text = "";
            TimeComboBox.SelectedIndex = 1;
        }

        private void EditMenuItemButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var item = button?.DataContext as MenuItemWithDetails;
            if (item != null)
            {
                var dialog = new InputDialog($"Редактирование блюда \"{item.Блюдо}\"\n\nКоличество порций:", "Редактирование");
                if (dialog.ShowDialog() == true && int.TryParse(dialog.Answer, out int newPortions) && newPortions > 0)
                {
                    item.Количество_порций = newPortions;
                    item.Калорийность_всего = item.Калорийность_на_порцию * newPortions;
                    UpdateMenuItemsDisplay();
                    UpdateStats();
                }
            }
        }

        private void DeleteMenuItemButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var item = button?.DataContext as MenuItemWithDetails;
            if (item != null)
            {
                var result = MessageBox.Show($"Удалить блюдо \"{item.Блюдо}\" из меню?",
                    "Подтверждение удаления", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    _menuItems.Remove(item);
                    // Перенумерация порядков
                    for (int i = 0; i < _menuItems.Count; i++)
                    {
                        _menuItems[i].Порядок_подачи = i + 1;
                    }
                    UpdateMenuItemsDisplay();
                    UpdateStats();
                }
            }
        }

        private void MoveUpButton_Click(object sender, RoutedEventArgs e)
        {
            // Находим выбранный элемент в DataGrid
            var selectedItem = FindSelectedMenuItem();
            if (selectedItem != null && selectedItem.Порядок_подачи > 1)
            {
                var itemAbove = _menuItems.FirstOrDefault(i => i.Порядок_подачи == selectedItem.Порядок_подачи - 1);
                if (itemAbove != null)
                {
                    int temp = selectedItem.Порядок_подачи;
                    selectedItem.Порядок_подачи = itemAbove.Порядок_подачи;
                    itemAbove.Порядок_подачи = temp;

                    UpdateMenuItemsDisplay();
                }
            }
        }

        private void MoveDownButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedItem = FindSelectedMenuItem();
            if (selectedItem != null && selectedItem.Порядок_подачи < _menuItems.Count)
            {
                var itemBelow = _menuItems.FirstOrDefault(i => i.Порядок_подачи == selectedItem.Порядок_подачи + 1);
                if (itemBelow != null)
                {
                    int temp = selectedItem.Порядок_подачи;
                    selectedItem.Порядок_подачи = itemBelow.Порядок_подачи;
                    itemBelow.Порядок_подачи = temp;

                    UpdateMenuItemsDisplay();
                }
            }
        }

        private MenuItemWithDetails FindSelectedMenuItem()
        {
            // Поиск выбранного элемента в группах
            var groups = MenuItemsControl.ItemsSource as IEnumerable<TimeGroup>;
            if (groups == null) return null;

            foreach (var group in groups)
            {
                foreach (var item in group.Items)
                {
                    // Проверяем, выбран ли этот элемент (упрощенно - берем первый выбранный)
                    return item;
                }
            }
            return null;
        }

        private void RecalculateCaloriesButton_Click(object sender, RoutedEventArgs e)
        {
            RecalculateCalories();
            MessageBox.Show($"Калорийность меню пересчитана.\nОбщая калорийность: {_totalCalories:F0} ккал",  // ИСПРАВЛЕНО
                "Пересчет завершен", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void PrintMenuButton_Click(object sender, RoutedEventArgs e)
        {
            if (!_menuId.HasValue && _isNewMenu)
            {
                MessageBox.Show("Сначала сохраните меню, затем распечатывайте его.",
                    "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (_menuId.HasValue)
            {
                try
                {
                    _menuPrinterService.ShowMenuInBrowser(_menuId.Value);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при печати меню: {ex.Message}", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void CreateRequestButton_Click(object sender, RoutedEventArgs e)
        {
            if (!_menuId.HasValue && _isNewMenu)
            {
                MessageBox.Show("Сначала сохраните меню, затем создавайте требование накладную.",
                    "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (_menuId.HasValue)
            {
                CreateRequestForMenu(_menuId.Value);
            }
        }

        private void CreateRequestForMenu(int menuId)
        {
            try
            {
                using (var db = new MenuStolovayaDBEntities())
                {
                    // Проверяем, нет ли уже требования
                    var existingRequest = db.Требования_накладные
                        .FirstOrDefault(tr => tr.Меню_id == menuId && tr.Статус_требования == "Ожидает");

                    if (existingRequest != null)
                    {
                        MessageBox.Show("Для этого меню уже создано требование, ожидающее обработки",
                            "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    string requestNumber = $"ТР-{DateTime.Now:yyyyMMdd}-{new Random().Next(1000, 9999)}";

                    var document = new Документы
                    {
                        Номер = requestNumber,
                        Тип_документа = "Требование",
                        Дата_документа = DateTime.Now,
                        Склад_отправитель_id = 1,
                        Статус = "Черновик",
                        Кто_создал_id = ThisUser.CurrentUser.Id,
                        Комментарий = $"Требование на меню ID {menuId}"
                    };
                    db.Документы.Add(document);
                    db.SaveChanges();

                    // Добавляем строки на основе блюд в меню
                    var menuItems = db.Строки_меню
                        .Where(sm => sm.Меню_id == menuId)
                        .ToList();

                    foreach (var item in menuItems)
                    {
                        var techCard = db.Технологические_карты
                            .FirstOrDefault(tc => tc.Блюдо_id == item.Блюдо_id);

                        if (techCard != null)
                        {
                            var recipes = db.Рецептуры
                                .Where(r => r.Технологическая_карта_id == techCard.id)
                                .ToList();

                            foreach (var recipe in recipes)
                            {
                                var product = db.Продукты.Find(recipe.Продукт_id);
                                if (product != null)
                                {
                                    decimal netto = (recipe.Количество_нетто ?? recipe.Количество_брутто) * (item.Количество_порций ?? 1);

                                    var existingLine = db.Строки_документов
                                        .FirstOrDefault(sd => sd.Документ_id == document.id && sd.Продукт_id == recipe.Продукт_id);

                                    if (existingLine != null)
                                    {
                                        existingLine.Количество += netto;
                                    }
                                    else
                                    {
                                        db.Строки_документов.Add(new Строки_документов
                                        {
                                            Документ_id = document.id,
                                            Продукт_id = recipe.Продукт_id,
                                            Количество = netto,
                                            Цена = product.Цена ?? 0
                                        });
                                    }
                                }
                            }
                        }
                    }

                    db.SaveChanges();

                    var request = new Требования_накладные
                    {
                        Номер = requestNumber,
                        Документ_id = document.id,
                        Меню_id = menuId,
                        Дата_требования = DateTime.Now,
                        Технолог_id = ThisUser.CurrentUser.Id,
                        Статус_требования = "Ожидает"
                    };
                    db.Требования_накладные.Add(request);
                    db.SaveChanges();

                    MessageBox.Show($"Требование накладная №{requestNumber} создана и отправлена кладовщику",
                        "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при создании требования: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SaveMenu()
        {
            try
            {
                if (!MenuDatePicker.SelectedDate.HasValue)
                {
                    MessageBox.Show("Выберите дату меню", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (!_menuItems.Any())
                {
                    MessageBox.Show("Добавьте хотя бы одно блюдо в меню", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                using (var db = new MenuStolovayaDBEntities())
                {
                    Меню_на_день menu;

                    if (_isNewMenu)
                    {
                        menu = new Меню_на_день
                        {
                            Дата = MenuDatePicker.SelectedDate.Value,
                            Ответственный_id = ThisUser.CurrentUser.Id,
                            Дата_составления = DateTime.Now,
                            Статус = ((ComboBoxItem)StatusComboBox.SelectedItem)?.Content?.ToString() ?? "Черновик"
                        };
                        db.Меню_на_день.Add(menu);
                        db.SaveChanges();
                        _menuId = menu.id;
                        _isNewMenu = false;

                        TitleText.Text = "Редактирование меню";
                        SubtitleText.Text = $"Меню от {menu.Дата:dd.MM.yyyy}";
                    }
                    else
                    {
                        menu = db.Меню_на_день.Find(_menuId.Value);
                        if (menu == null) return;

                        menu.Дата = MenuDatePicker.SelectedDate.Value;
                        menu.Статус = ((ComboBoxItem)StatusComboBox.SelectedItem)?.Content?.ToString() ?? "Черновик";
                    }

                    // Удаляем старые строки
                    var oldItems = db.Строки_меню.Where(sm => sm.Меню_id == menu.id).ToList();
                    foreach (var oldItem in oldItems)
                    {
                        db.Строки_меню.Remove(oldItem);
                    }

                    // Добавляем новые строки
                    foreach (var item in _menuItems)
                    {
                        db.Строки_меню.Add(new Строки_меню
                        {
                            Меню_id = menu.id,
                            Блюдо_id = item.Блюдо_id,
                            Количество_порций = item.Количество_порций,
                            Выход_на_порцию = item.Выход_на_порцию,
                            Время_подачи = item.Время_подачи,
                            Порядок_подачи = item.Порядок_подачи
                        });
                    }

                    // Обновляем общую калорийность
                    menu.Калорийность_общая = _totalCalories;

                    db.SaveChanges();
                }

                MessageBox.Show("Меню успешно сохранено", "Успех",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении меню: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            SaveMenu();
            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex(@"^[0-9]*$");
            e.Handled = !regex.IsMatch(e.Text);
        }
    }

    // Класс для группировки по времени - ИСПРАВЛЕНО
    public class TimeGroup
    {
        public string Name { get; set; }  // ← Переименовано с TimeGroup на Name
        public List<MenuItemWithDetails> Items { get; set; }
    }

    // Расширенный класс для блюда в меню
    public class MenuItemWithDetails
    {
        public int Id { get; set; }
        public int Блюдо_id { get; set; }
        public string Блюдо { get; set; }
        public string Вид_блюда { get; set; }
        public int Количество_порций { get; set; }
        public decimal Выход_на_порцию { get; set; }
        public string Время_подачи { get; set; }
        public int Порядок_подачи { get; set; }
        public decimal Калорийность_брюда { get; set; }
        public decimal Калорийность_на_порцию { get; set; }
        public decimal Калорийность_всего { get; set; }
    }
}