using MenuStolovaya.Models;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace MenuStolovaya.Views
{
    public partial class EditProductPriceDialog : Window
    {
        private int _productId;
        private decimal _currentPrice;
        private decimal _newPrice;

        public EditProductPriceDialog(int productId, decimal currentPrice)
        {
            InitializeComponent();
            _productId = productId;
            _currentPrice = currentPrice;
            LoadProductData();
        }

        private void LoadProductData()
        {
            try
            {
                using (var db = new MenuStolovayaDBEntities())
                {
                    var product = db.Продукты
                        .Include("Категории_продуктов")
                        .FirstOrDefault(p => p.id == _productId);

                    if (product == null)
                    {
                        MessageBox.Show("Продукт не найден",
                            "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        this.Close();
                        return;
                    }

                    ProductNameText.Text = product.Наименование;
                    ArticleText.Text = product.Артикул ?? "Не указан";
                    CategoryText.Text = product.Категории_продуктов?.Наименование ?? "Без категории";
                    UnitText.Text = product.Единица_измерения;

                    CurrentPriceText.Text = $"{_currentPrice:N2} руб.";
                    NewPriceTextBox.Text = _currentPrice.ToString("N2");

                    UpdateChangeText(_currentPrice);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке данных продукта: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                this.Close();
            }
        }

        private void PriceTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // Разрешаем цифры, точку и запятую
            foreach (char ch in e.Text)
            {
                if (!char.IsDigit(ch) && ch != '.' && ch != ',')
                {
                    e.Handled = true;
                    return;
                }
            }

            // Проверка на два разделителя
            TextBox textBox = sender as TextBox;
            if (textBox != null)
            {
                string currentText = textBox.Text;
                string newText = currentText.Insert(textBox.SelectionStart, e.Text);

                int dotCount = newText.Count(c => c == '.');
                int commaCount = newText.Count(c => c == ',');

                if (dotCount > 1 || commaCount > 1 || (dotCount > 0 && commaCount > 0))
                {
                    e.Handled = true;
                }
            }
        }

        private void NewPriceTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (decimal.TryParse(NewPriceTextBox.Text, out decimal newPrice))
            {
                _newPrice = newPrice;
                UpdateChangeText(newPrice);
            }
        }

        private void UpdateChangeText(decimal newPrice)
        {
            decimal change = newPrice - _currentPrice;
            decimal percentChange = _currentPrice > 0 ? (change / _currentPrice) * 100 : 0;

            if (change > 0)
            {
                ChangeText.Text = $"+{change:N2} руб. (+{percentChange:N1}%)";
                ChangeText.Foreground = System.Windows.Media.Brushes.Red;
            }
            else if (change < 0)
            {
                ChangeText.Text = $"{change:N2} руб. ({percentChange:N1}%)";
                ChangeText.Foreground = System.Windows.Media.Brushes.Green;
            }
            else
            {
                ChangeText.Text = "Без изменений";
                ChangeText.Foreground = System.Windows.Media.Brushes.Gray;
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (!decimal.TryParse(NewPriceTextBox.Text, out decimal newPrice))
            {
                MessageBox.Show("Введите корректную цену",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (newPrice <= 0)
            {
                MessageBox.Show("Цена должна быть больше нуля",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (newPrice == _currentPrice)
            {
                MessageBox.Show("Цена не изменилась",
                    "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                this.DialogResult = false;
                this.Close();
                return;
            }

            try
            {
                using (var db = new MenuStolovayaDBEntities())
                {
                    var product = db.Продукты.Find(_productId);
                    if (product == null) return;

                    product.Цена = newPrice;
                    product.Утверждена_цена = false; // Сбрасываем статус утверждения
                    product.Кто_утвердил_цену_id = null;
                    product.Дата_утверждения_цены = null;

                    db.SaveChanges();
                }

                this.DialogResult = true;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при изменении цены: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}