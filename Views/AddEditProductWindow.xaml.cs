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
using System.Globalization;

namespace MenuStolovaya.Views
{
    public partial class AddEditProductWindow : Window
    {
        public ProductModel Product { get; private set; }
        public string WindowTitle { get; private set; }
        public List<string> Units { get; private set; }

        // Свойства для текстовых представлений чисел
        public string ColdLossText { get; set; }
        public string HotLossText { get; set; }
        public string ProteinsText { get; set; }
        public string FatsText { get; set; }
        public string CarbsText { get; set; }
        public string CalorieText { get; set; }
        public string PriceText { get; set; }

        public AddEditProductWindow(Продукты product = null)
        {
            InitializeComponent();

            Units = new List<string> { "кг", "г", "л", "мл", "шт" };

            if (product == null)
            {
                Product = new ProductModel
                {
                    Активен = true,
                    Утверждена_цена = false,
                    Потери_холодной_обработки = 0,
                    Потери_горячей_обработки = 0,
                    Цена = 0
                };
                WindowTitle = "Добавление продукта";

                // Инициализируем текстовые поля пустыми значениями
                ColdLossText = "0";
                HotLossText = "0";
                ProteinsText = "";
                FatsText = "";
                CarbsText = "";
                CalorieText = "";
                PriceText = "0";
            }
            else
            {
                Product = new ProductModel
                {
                    Id = product.id,
                    Артикул = product.Артикул,
                    Наименование = product.Наименование,
                    Категория_id = product.Категория_id,
                    Единица_измерения = product.Единица_измерения,
                    Потери_холодной_обработки = product.Потери_холодной_обработки ?? 0,
                    Потери_горячей_обработки = product.Потери_горячей_обработки ?? 0,
                    Белки = product.Белки,
                    Жиры = product.Жиры,
                    Углеводы = product.Углеводы,
                    Калорийность = product.Калорийность,
                    Цена = product.Цена ?? 0,
                    Утверждена_цена = product.Утверждена_цена ?? false,
                    Активен = product.Активен ?? true
                };
                WindowTitle = "Редактирование продукта";

                // Преобразуем числовые значения в строки с учетом культуры
                ColdLossText = FormatDecimalForDisplay(product.Потери_холодной_обработки ?? 0);
                HotLossText = FormatDecimalForDisplay(product.Потери_горячей_обработки ?? 0);
                ProteinsText = FormatDecimalForDisplay(product.Белки);
                FatsText = FormatDecimalForDisplay(product.Жиры);
                CarbsText = FormatDecimalForDisplay(product.Углеводы);
                CalorieText = FormatDecimalForDisplay(product.Калорийность);
                PriceText = FormatDecimalForDisplay(product.Цена ?? 0);
            }

            DataContext = this;
            LoadCategories();
        }

        private string FormatDecimalForDisplay(decimal? value)
        {
            if (!value.HasValue) return "";

            // Форматируем с учетом текущей культуры
            return value.Value.ToString(CultureInfo.CurrentCulture);
        }

        private string FormatDecimalForDisplay(decimal value)
        {
            // Форматируем с учетом текущей культуры
            return value.ToString(CultureInfo.CurrentCulture);
        }

        private decimal? ParseDecimalFromText(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return null;

            text = text.Trim();

            // Заменяем точку на запятую для совместимости
            text = text.Replace('.', ',');

            // Пробуем распарсить с учетом текущей культуры
            if (decimal.TryParse(text, NumberStyles.Any, CultureInfo.CurrentCulture, out decimal result))
            {
                return result;
            }

            // Если не получилось с текущей культурой, пробуем инвариантную
            if (decimal.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out result))
            {
                return result;
            }

            return null;
        }

        private void LoadCategories()
        {
            try
            {
                using (var db = new MenuStolovayaDBEntities())
                {
                    var categories = db.Категории_продуктов
                        .Select(c => new
                        {
                            Id = c.id,
                            Name = c.Наименование
                        })
                        .ToList();

                    CategoryComboBox.ItemsSource = categories;
                    CategoryComboBox.DisplayMemberPath = "Name";
                    CategoryComboBox.SelectedValuePath = "Id";

                    if (Product.Id > 0 && Product.Категория_id.HasValue)
                    {
                        CategoryComboBox.SelectedValue = Product.Категория_id.Value;
                    }
                    else if (categories.Any())
                    {
                        CategoryComboBox.SelectedIndex = 0;
                        Product.Категория_id = (int)CategoryComboBox.SelectedValue;
                    }
                }

                if (!string.IsNullOrEmpty(Product.Единица_измерения))
                {
                    UnitComboBox.SelectedItem = Product.Единица_измерения;
                }
                else
                {
                    UnitComboBox.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке категорий: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateProductFromTextFields()
        {
            // Обновляем числовые поля продукта из текстовых полей
            Product.Потери_холодной_обработки = ParseDecimalFromText(ColdLossText) ?? 0;
            Product.Потери_горячей_обработки = ParseDecimalFromText(HotLossText) ?? 0;
            Product.Белки = ParseDecimalFromText(ProteinsText);
            Product.Жиры = ParseDecimalFromText(FatsText);
            Product.Углеводы = ParseDecimalFromText(CarbsText);
            Product.Калорийность = ParseDecimalFromText(CalorieText);
            Product.Цена = ParseDecimalFromText(PriceText) ?? 0;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Обновляем числовые значения из текстовых полей
                UpdateProductFromTextFields();

                if (string.IsNullOrWhiteSpace(Product.Артикул))
                {
                    MessageBox.Show("Введите артикул продукта", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (string.IsNullOrWhiteSpace(Product.Наименование))
                {
                    MessageBox.Show("Введите наименование продукта", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (Product.Категория_id == null || Product.Категория_id <= 0)
                {
                    MessageBox.Show("Выберите категорию", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (string.IsNullOrWhiteSpace(Product.Единица_измерения))
                {
                    MessageBox.Show("Выберите единицу измерения", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Проверка числовых полей
                if (Product.Потери_холодной_обработки < 0 || Product.Потери_холодной_обработки > 100)
                {
                    MessageBox.Show("Потери холодной обработки должны быть в диапазоне от 0 до 100%", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (Product.Потери_горячей_обработки < 0 || Product.Потери_горячей_обработки > 100)
                {
                    MessageBox.Show("Потери горячей обработки должны быть в диапазоне от 0 до 100%", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (Product.Белки.HasValue && Product.Белки.Value < 0)
                {
                    MessageBox.Show("Белки не могут быть отрицательными", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (Product.Жиры.HasValue && Product.Жиры.Value < 0)
                {
                    MessageBox.Show("Жиры не могут быть отрицательными", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (Product.Углеводы.HasValue && Product.Углеводы.Value < 0)
                {
                    MessageBox.Show("Углеводы не могут быть отрицательными", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (Product.Калорийность.HasValue && Product.Калорийность.Value < 0)
                {
                    MessageBox.Show("Калорийность не может быть отрицательной", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (Product.Цена < 0)
                {
                    MessageBox.Show("Цена не может быть отрицательной", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void CategoryComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CategoryComboBox.SelectedValue != null && CategoryComboBox.SelectedValue is int)
            {
                Product.Категория_id = (int)CategoryComboBox.SelectedValue;
            }
        }

        private void UnitComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (UnitComboBox.SelectedItem != null)
            {
                Product.Единица_измерения = UnitComboBox.SelectedItem.ToString();
            }
        }

        private void CalculateCaloriesButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Обновляем значения из текстовых полей
                Product.Белки = ParseDecimalFromText(ProteinsText);
                Product.Жиры = ParseDecimalFromText(FatsText);
                Product.Углеводы = ParseDecimalFromText(CarbsText);

                if (Product.Белки.HasValue && Product.Жиры.HasValue && Product.Углеводы.HasValue)
                {
                    // Рассчитываем калорийность
                    decimal calories = Product.Белки.Value * 4 +
                                      Product.Жиры.Value * 9 +
                                      Product.Углеводы.Value * 4;

                    Product.Калорийность = calories;
                    CalorieText = FormatDecimalForDisplay(calories);

                    // Обновляем привязку
                    var binding = CalorieTextBox.GetBindingExpression(TextBox.TextProperty);
                    if (binding != null)
                    {
                        binding.UpdateTarget();
                    }

                    MessageBox.Show($"Калорийность рассчитана: {calories:F1} ккал/100г",
                        "Расчет калорийности", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("Заполните значения БЖУ для расчета калорийности",
                        "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при расчете калорийности: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}