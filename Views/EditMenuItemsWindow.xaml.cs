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
using MenuStolovaya.Models;
using MenuStolovaya.Services;
using System.Text.RegularExpressions;
namespace MenuStolovaya.Views
{
    public partial class EditMenuItemsWindow : Window
    {
        private int _menuId;
        private MenuService _menuService;
        private decimal _totalMenuCalories = 0;

        public EditMenuItemsWindow(int menuId)
        {
            InitializeComponent();
            _menuId = menuId;
            _menuService = new MenuService();

            LoadData();
        }

        private void LoadData()
        {
            try
            {
                // Загружаем информацию о меню
                using (var db = new MenuStolovayaDBEntities())
                {
                    var menu = db.Меню_на_день.Find(_menuId);
                    if (menu != null)
                    {
                        MenuInfoText.Text = $"Меню на дату: {menu.Дата:dd.MM.yyyy}";
                        _totalMenuCalories = menu.Калорийность_общая ?? 0;
                        UpdateCaloriesInfo();
                    }

                    // Загружаем блюда для выпадающего списка с дополнительной информацией
                    var dishes = db.Блюда
                        .Where(d => d.Активно == true)
                        .Select(d => new DishDisplayInfo
                        {
                            Id = d.id,
                            Наименование = d.Наименование,
                            Калорийность_расчетная = d.Калорийность_расчетная,
                            Выход_стандартный = d.Выход_стандартный ?? 100
                        })
                        .ToList();

                    DishComboBox.ItemsSource = dishes;
                    DishComboBox.DisplayMemberPath = "Наименование";
                    DishComboBox.SelectedValuePath = "Id";
                }

                // Загружаем строки меню
                LoadMenuItems();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке данных: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadMenuItems()
        {
            try
            {
                using (var db = new MenuStolovayaDBEntities())
                {
                    var menuItemsData = db.Строки_меню
                        .Where(sm => sm.Меню_id == _menuId)
                        .Join(db.Блюда,
                            sm => sm.Блюдо_id,
                            b => b.id,
                            (sm, b) => new MenuItemDisplayFull
                            {
                                Id = sm.id,
                                Меню_id = _menuId, // Добавляем ID меню
                                Блюдо_id = b.id,
                                Блюдо = b.Наименование,
                                Вид_блюда = b.Виды_блюд != null ? b.Виды_блюд.Наименование : null,
                                Количество_порций = sm.Количество_порций ?? 1,
                                Выход_на_порцию = sm.Выход_на_порцию ?? b.Выход_стандартный ?? 100,
                                Время_подачи = sm.Время_подачи,
                                Порядок_подачи = sm.Порядок_подачи ?? 0,
                                Калорийность_брюда = b.Калорийность_расчетная ?? 0,
                                Калорийность_на_порцию = 0, // Рассчитаем ниже
                                Калорийность_всего = 0 // Рассчитаем ниже
                            })
                        .ToList();

                    // Рассчитываем калорийность для каждой строки меню
                    foreach (var item in menuItemsData)
                    {
                        // Калорийность на 100г * выход порции в г / 100
                        decimal калорийностьНаПорцию = (item.Калорийность_брюда / 100m) * item.Выход_на_порцию ?? 100;
                        item.Калорийность_на_порцию = калорийностьНаПорцию;
                        item.Калорийность_всего = калорийностьНаПорцию * item.Количество_порций;
                    }

                    MenuItemsDataGrid.ItemsSource = menuItemsData.OrderBy(m => m.Порядок_подачи).ToList();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке строк меню: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DishComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DishComboBox.SelectedItem is DishDisplayInfo selectedDish)
            {
                // Показываем калорийность блюда
                DishCaloriesText.Text = $"{selectedDish.Калорийность_расчетная ?? 0:F1} калорий/100г";

                // Показываем стандартный выход блюда
                StandardOutputText.Text = $"{selectedDish.Выход_стандартный:F0} г";

                // Автоматически подставляем стандартный выход в поле выхода
                OutputTextBox.Text = selectedDish.Выход_стандартный.ToString("F0");
            }
        }

        private void UseStandardOutputButton_Click(object sender, RoutedEventArgs e)
        {
            if (DishComboBox.SelectedItem is DishDisplayInfo selectedDish)
            {
                OutputTextBox.Text = selectedDish.Выход_стандартный.ToString("F0");
            }
        }

        // Валидация ввода целых чисел
        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex(@"^[0-9]+$");
            e.Handled = !regex.IsMatch(e.Text);
        }

        // Валидация ввода десятичных чисел
        private void DecimalValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex(@"^[0-9]*(?:\.[0-9]*)?$");
            e.Handled = !regex.IsMatch((sender as TextBox).Text.Insert((sender as TextBox).SelectionStart, e.Text));
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
                PortionsTextBox.Focus();
                return;
            }

            if (!decimal.TryParse(OutputTextBox.Text, out decimal output) || output <= 0)
            {
                MessageBox.Show("Введите корректный выход на порцию", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                OutputTextBox.Focus();
                return;
            }

            try
            {
                var dish = (DishDisplayInfo)DishComboBox.SelectedItem;
                var timeItem = TimeComboBox.SelectedItem as ComboBoxItem;
                var time = timeItem?.Content?.ToString() ?? "Обед";

                var menuItem = new MenuItemModel
                {
                    Меню_id = _menuId,
                    Блюдо_id = dish.Id,
                    Количество_порций = portions,
                    Выход_на_порцию = output,
                    Время_подачи = time
                };

                if (_menuService.AddMenuItem(menuItem))
                {
                    // Пересчитываем общую калорийность меню
                    UpdateMenuTotalCalories();

                    LoadMenuItems();
                    DishComboBox.SelectedIndex = -1;
                    PortionsTextBox.Text = "1";
                    OutputTextBox.Text = "";
                    DishCaloriesText.Text = "";
                    StandardOutputText.Text = "";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при добавлении блюда: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateMenuTotalCalories()
        {
            try
            {
                using (var db = new MenuStolovayaDBEntities())
                {
                    // Рассчитываем общую калорийность меню
                    decimal totalCalories = db.Строки_меню
                        .Where(sm => sm.Меню_id == _menuId)
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
                    var menu = db.Меню_на_день.Find(_menuId);
                    if (menu != null)
                    {
                        menu.Калорийность_общая = totalCalories;
                        db.SaveChanges();
                        _totalMenuCalories = totalCalories;
                        UpdateCaloriesInfo();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при расчете калорийности меню: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateCaloriesInfo()
        {
            CaloriesInfoText.Text = $"Общая калорийность: {_totalMenuCalories:F0} калорий";
        }

        private void EditMenuItemButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedItem = MenuItemsDataGrid.SelectedItem as MenuItemDisplayFull;
            if (selectedItem != null)
            {
                // Открываем диалоговое окно для редактирования
                var editWindow = new EditMenuItemWindow(selectedItem);
                if (editWindow.ShowDialog() == true)
                {
                    if (_menuService.UpdateMenuItem(editWindow.MenuItem))
                    {
                        // Пересчитываем общую калорийность
                        UpdateMenuTotalCalories();
                        LoadMenuItems();
                    }
                }
            }
        }

        private void DeleteMenuItemButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedItem = MenuItemsDataGrid.SelectedItem as MenuItemDisplayFull;
            if (selectedItem != null)
            {
                var result = MessageBox.Show($"Удалить блюдо \"{selectedItem.Блюдо}\" из меню?",
                    "Подтверждение удаления", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    if (_menuService.DeleteMenuItem(selectedItem.Id))
                    {
                        // Пересчитываем общую калорийность
                        UpdateMenuTotalCalories();
                        LoadMenuItems();
                    }
                }
            }
        }

        private void MoveUpButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedItem = MenuItemsDataGrid.SelectedItem as MenuItemDisplayFull;
            if (selectedItem != null && selectedItem.Порядок_подачи > 1)
            {
                try
                {
                    using (var db = new MenuStolovayaDBEntities())
                    {
                        // Находим блюдо выше
                        var itemAbove = db.Строки_меню
                            .FirstOrDefault(sm => sm.Меню_id == _menuId &&
                                                sm.Порядок_подачи == selectedItem.Порядок_подачи - 1);

                        var currentItem = db.Строки_меню.Find(selectedItem.Id);

                        if (itemAbove != null && currentItem != null)
                        {
                            // Меняем местами порядковые номера
                            int temp = itemAbove.Порядок_подачи ?? 0;
                            itemAbove.Порядок_подачи = currentItem.Порядок_подачи ?? 0;
                            currentItem.Порядок_подачи = temp;

                            db.SaveChanges();
                            LoadMenuItems();
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при перемещении: {ex.Message}", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void MoveDownButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedItem = MenuItemsDataGrid.SelectedItem as MenuItemDisplayFull;
            if (selectedItem != null)
            {
                try
                {
                    using (var db = new MenuStolovayaDBEntities())
                    {
                        // Находим блюдо ниже
                        var itemBelow = db.Строки_меню
                            .FirstOrDefault(sm => sm.Меню_id == _menuId &&
                                                sm.Порядок_подачи == selectedItem.Порядок_подачи + 1);

                        var currentItem = db.Строки_меню.Find(selectedItem.Id);

                        if (itemBelow != null && currentItem != null)
                        {
                            // Меняем местами порядковые номера
                            int temp = itemBelow.Порядок_подачи ?? 0;
                            itemBelow.Порядок_подачи = currentItem.Порядок_подачи ?? 0;
                            currentItem.Порядок_подачи = temp;

                            db.SaveChanges();
                            LoadMenuItems();
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при перемещении: {ex.Message}", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            // Обновляем общую калорийность перед закрытием
            UpdateMenuTotalCalories();
            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }

    // Вспомогательный класс для отображения информации о блюде
    public class DishDisplayInfo
    {
        public int Id { get; set; }
        public string Наименование { get; set; }
        public decimal? Калорийность_расчетная { get; set; }
        public decimal Выход_стандартный { get; set; }
    }

    // Расширенный класс для отображения строк меню
    public class MenuItemDisplayFull
    {
        public int Id { get; set; }
        public int Меню_id { get; set; } // Добавлено свойство Меню_id
        public int Блюдо_id { get; set; }
        public string Блюдо { get; set; }
        public string Вид_блюда { get; set; }
        public int Количество_порций { get; set; }
        public decimal? Выход_на_порцию { get; set; }
        public string Время_подачи { get; set; }
        public int Порядок_подачи { get; set; }
        public decimal Калорийность_брюда { get; set; }
        public decimal Калорийность_на_порцию { get; set; }
        public decimal Калорийность_всего { get; set; }
    }
}