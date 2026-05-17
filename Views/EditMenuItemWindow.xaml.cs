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

namespace MenuStolovaya.Views
{
    public partial class EditMenuItemWindow : Window
    {
        public MenuItemModel MenuItem { get; private set; }

        public EditMenuItemWindow(MenuItemDisplayFull menuItemDisplay)
        {
            InitializeComponent();

            MenuItem = new MenuItemModel
            {
                Id = menuItemDisplay.Id,
                Меню_id = menuItemDisplay.Меню_id, // Используем правильное свойство
                Блюдо_id = menuItemDisplay.Блюдо_id,
                Количество_порций = menuItemDisplay.Количество_порций,
                Выход_на_порцию = menuItemDisplay.Выход_на_порцию ?? 100,
                Время_подачи = menuItemDisplay.Время_подачи,
                Порядок_подачи = menuItemDisplay.Порядок_подачи
            };

            // Заполняем поля
            DishNameText.Text = menuItemDisplay.Блюдо;
            PortionsTextBox.Text = menuItemDisplay.Количество_порций.ToString();
            OutputTextBox.Text = (menuItemDisplay.Выход_на_порцию ?? 100).ToString("F0");

            // Устанавливаем время подачи
            foreach (ComboBoxItem item in TimeComboBox.Items)
            {
                if (item.Content.ToString() == menuItemDisplay.Время_подачи)
                {
                    TimeComboBox.SelectedItem = item;
                    break;
                }
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

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            // Валидация
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

            // Обновляем данные
            MenuItem.Количество_порций = portions;
            MenuItem.Выход_на_порцию = output;
            MenuItem.Время_подачи = (TimeComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Обед";

            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}