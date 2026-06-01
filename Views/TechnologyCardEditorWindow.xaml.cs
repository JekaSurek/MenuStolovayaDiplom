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
using System.Text.RegularExpressions;
using System.IO;
using System.Diagnostics;
using MenuStolovaya.Models;
using MenuStolovaya.Services;

namespace MenuStolovaya.Views
{
    public partial class TechnologyCardEditorWindow : Window
    {
        private int? _cardId;
        private bool _isNewCard;
        private TechnologyCardService _techCardService;
        private RecipeService _recipeService;
        private TechnologyCardModel _currentCard;
        private List<RecipeDisplay> _currentRecipes;

        public TechnologyCardEditorWindow(int? cardId = null)
        {
            InitializeComponent();
            _cardId = cardId;
            _isNewCard = !cardId.HasValue;
            _techCardService = new TechnologyCardService();
            _recipeService = new RecipeService();

            DataContext = this;
            LoadData();
        }

        // Свойства для привязки
        public bool IsNewCard => _isNewCard;
        public bool CanChangeStatus => !_isNewCard && (ThisUser.IsTechnologist || ThisUser.IsAdmin);

        private void LoadData()
        {
            LoadDishes();
            LoadProducts();

            if (!_isNewCard && _cardId.HasValue)
            {
                LoadCardData();
                LoadRecipes();
            }
            else
            {
                // Новая карта
                TitleText.Text = "Новая технологическая карта";
                SubtitleText.Text = "Заполните информацию о блюде и добавьте ингредиенты";
                CardNumberTextBox.Text = GenerateCardNumber();
                StatusComboBox.SelectedIndex = 0;
            }
        }

        private string GenerateCardNumber()
        {
            return $"ТК-{DateTime.Now:yyyyMMdd}-{new Random().Next(1000, 9999)}";
        }

        private void LoadDishes()
        {
            try
            {
                using (var db = new MenuStolovayaDBEntities())
                {
                    var dishes = db.Блюда
                        .Where(d => d.Активно == true)
                        .Select(d => new { Id = d.id, Name = d.Наименование, StandardOutput = d.Выход_стандартный ?? 100 })
                        .ToList();

                    DishComboBox.ItemsSource = dishes;
                    DishComboBox.DisplayMemberPath = "Name";
                    DishComboBox.SelectedValuePath = "Id";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки блюд: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadProducts()
        {
            try
            {
                using (var db = new MenuStolovayaDBEntities())
                {
                    // Загружаем данные из БД без форматирования
                    var productsRaw = db.Продукты
                        .Where(p => p.Активен == true)
                        .Select(p => new
                        {
                            p.id,
                            p.Артикул,
                            p.Наименование,
                            p.Единица_измерения,
                            p.Потери_холодной_обработки,
                            p.Потери_горячей_обработки
                        })
                        .ToList();

                    // Форматируем DisplayText в памяти (не в LINQ to Entities)
                    var products = productsRaw.Select(p => new
                    {
                        p.id,
                        DisplayText = $"{p.Артикул} - {p.Наименование}",
                        p.Единица_измерения,
                        p.Потери_холодной_обработки,
                        p.Потери_горячей_обработки
                    }).ToList();

                    ProductComboBox.ItemsSource = products;
                    ProductComboBox.DisplayMemberPath = "DisplayText";
                    ProductComboBox.SelectedValuePath = "id";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки продуктов: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadCardData()
        {
            try
            {
                _currentCard = _techCardService.GetTechnologyCardById(_cardId.Value);
                if (_currentCard == null) return;

                TitleText.Text = $"Редактирование технологической карты";
                SubtitleText.Text = $"Карта №{_currentCard.Номер} | Создана: {_currentCard.Дата_создания:dd.MM.yyyy}";

                CardNumberTextBox.Text = _currentCard.Номер;
                DishComboBox.SelectedValue = _currentCard.Блюдо_id;
                OutputTextBox.Text = _currentCard.Выход.ToString("N1");
                TechnologyTextBox.Text = _currentCard.Технология_приготовления;

                StatusComboBox.SelectedItem = StatusComboBox.Items
                    .Cast<ComboBoxItem>()
                    .FirstOrDefault(i => i.Content.ToString() == _currentCard.Статус);

                // Загружаем время приготовления из блюда
                using (var db = new MenuStolovayaDBEntities())
                {
                    var dish = db.Блюда.Find(_currentCard.Блюдо_id);
                    if (dish != null)
                    {
                        CookingTimeTextBox.Text = (dish.Время_приготовления ?? 30).ToString();
                    }
                }

                UpdateCaloriesDisplay();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки карты: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadRecipes()
        {
            if (!_cardId.HasValue) return;

            _currentRecipes = _recipeService.GetRecipes(_cardId.Value);
            RecipeDataGrid.ItemsSource = _currentRecipes;
            UpdateSummaryInfo();
        }

        private void DishComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DishComboBox.SelectedItem != null)
            {
                dynamic selected = DishComboBox.SelectedItem;
                int dishId = selected.Id;

                using (var db = new MenuStolovayaDBEntities())
                {
                    var dish = db.Блюда.Find(dishId);
                    if (dish != null)
                    {
                        // Подставляем стандартный выход
                        if (_isNewCard && string.IsNullOrEmpty(OutputTextBox.Text))
                        {
                            OutputTextBox.Text = (dish.Выход_стандартный ?? 100).ToString("N1");
                        }

                        // Подставляем время приготовления
                        CookingTimeTextBox.Text = (dish.Время_приготовления ?? 30).ToString();
                    }
                }
            }
        }

        private void ProductComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Можно показать дополнительную информацию о продукте при выборе
        }

        private void CalculateOutputButton_Click(object sender, RoutedEventArgs e)
        {
            if (!_cardId.HasValue)
            {
                MessageBox.Show("Сначала сохраните карту, чтобы рассчитать выход", "Внимание",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            decimal calculatedOutput = CalorieCalculator.CalculateDishOutput(_cardId.Value);
            if (calculatedOutput > 0)
            {
                OutputTextBox.Text = calculatedOutput.ToString("N1");
                MessageBox.Show($"Рекомендуемый выход блюда: {calculatedOutput:N1} г", "Расчет выхода",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show("Не удалось рассчитать выход. Добавьте ингредиенты в рецептуру.", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void UpdateCaloriesDisplay()
        {
            if (_cardId.HasValue)
            {
                using (var db = new MenuStolovayaDBEntities())
                {
                    var techCard = db.Технологические_карты.Find(_cardId.Value);
                    if (techCard != null)
                    {
                        var dish = db.Блюда.Find(techCard.Блюдо_id);
                        if (dish != null && dish.Калорийность_расчетная.HasValue)
                        {
                            CaloriesText.Text = $"{dish.Калорийность_расчетная.Value:F1} ккал / 100г";
                        }
                        else
                        {
                            CaloriesText.Text = "Не рассчитана";
                        }
                    }
                }
            }
            else
            {
                CaloriesText.Text = "Будет рассчитана после сохранения";
            }
        }

        private void UpdateSummaryInfo()
        {
            if (_currentRecipes == null || !_currentRecipes.Any())
            {
                TotalWeightText.Text = "Общий вес: 0 г";
                TotalCaloriesText.Text = "Общая калорийность: 0 ккал";
                return;
            }

            decimal totalNettoGrams = _currentRecipes.Sum(r => r.Количество_нетто * 1000);
            TotalWeightText.Text = $"Общий вес нетто: {totalNettoGrams:N1} г";

            if (_cardId.HasValue)
            {
                decimal totalCalories = CalorieCalculator.CalculateTotalDishCaloriesInKcal(
                    _currentCard?.Блюдо_id ?? 0);
                TotalCaloriesText.Text = $"Общая калорийность: {totalCalories:F2} ккал";
            }
        }

        private void AddProductButton_Click(object sender, RoutedEventArgs e)
        {
            if (!_cardId.HasValue)
            {
                MessageBox.Show("Сначала сохраните технологическую карту, затем добавляйте ингредиенты.",
                    "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (ProductComboBox.SelectedItem == null)
            {
                MessageBox.Show("Выберите продукт", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!decimal.TryParse(QuantityTextBox.Text, out decimal quantity) || quantity <= 0)
            {
                MessageBox.Show("Введите корректное количество в килограммах", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!int.TryParse(OrderTextBox.Text, out int order) || order <= 0)
            {
                MessageBox.Show("Введите корректный порядковый номер", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                dynamic selected = ProductComboBox.SelectedItem;
                int productId = selected.id;

                var recipe = new RecipeModel
                {
                    Технологическая_карта_id = _cardId.Value,
                    Продукт_id = productId,
                    Количество_брутто = quantity,
                    Порядок_закладки = order
                };

                if (_recipeService.AddRecipe(recipe))
                {
                    LoadRecipes();
                    ProductComboBox.SelectedIndex = -1;
                    QuantityTextBox.Text = "0";
                    int nextOrder = (_currentRecipes?.Max(r => (int?)r.Порядок_закладки) ?? 0) + 1;
                    OrderTextBox.Text = nextOrder.ToString();

                    UpdateDishCalculations();
                    UpdateCaloriesDisplay();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при добавлении продукта: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void EditRecipeButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var recipe = button?.DataContext as RecipeDisplay;
            if (recipe != null)
            {
                var dialog = new InputDialog("Введите новое количество (кг):", "Редактирование количества");
                if (dialog.ShowDialog() == true && decimal.TryParse(dialog.Answer, out decimal newQuantity) && newQuantity > 0)
                {
                    var updatedRecipe = new RecipeModel
                    {
                        Id = recipe.Id,
                        Технологическая_карта_id = _cardId.Value,
                        Продукт_id = recipe.Продукт_id,
                        Количество_брутто = newQuantity,
                        Порядок_закладки = recipe.Порядок_закладки
                    };

                    if (_recipeService.UpdateRecipe(updatedRecipe))
                    {
                        LoadRecipes();
                        UpdateDishCalculations();
                        UpdateCaloriesDisplay();
                    }
                }
            }
        }

        private void DeleteRecipeButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var recipe = button?.DataContext as RecipeDisplay;
            if (recipe != null)
            {
                var result = MessageBox.Show($"Удалить продукт \"{recipe.Продукт}\" из рецептуры?",
                    "Подтверждение удаления", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    if (_recipeService.DeleteRecipe(recipe.Id))
                    {
                        LoadRecipes();
                        _recipeService.UpdateRecipeOrder(_cardId.Value);
                        UpdateDishCalculations();
                        UpdateCaloriesDisplay();
                    }
                }
            }
        }

        private void MoveUpButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedRecipe = RecipeDataGrid.SelectedItem as RecipeDisplay;
            if (selectedRecipe != null && selectedRecipe.Порядок_закладки > 1)
            {
                SwapOrders(selectedRecipe.Порядок_закладки, selectedRecipe.Порядок_закладки - 1);
            }
        }

        private void MoveDownButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedRecipe = RecipeDataGrid.SelectedItem as RecipeDisplay;
            if (selectedRecipe != null && _currentRecipes != null)
            {
                int maxOrder = _currentRecipes.Max(r => r.Порядок_закладки);
                if (selectedRecipe.Порядок_закладки < maxOrder)
                {
                    SwapOrders(selectedRecipe.Порядок_закладки, selectedRecipe.Порядок_закладки + 1);
                }
            }
        }

        private void SwapOrders(int order1, int order2)
        {
            try
            {
                using (var db = new MenuStolovayaDBEntities())
                {
                    var recipe1 = db.Рецептуры.FirstOrDefault(r => r.Технологическая_карта_id == _cardId && r.Порядок_закладки == order1);
                    var recipe2 = db.Рецептуры.FirstOrDefault(r => r.Технологическая_карта_id == _cardId && r.Порядок_закладки == order2);

                    if (recipe1 != null && recipe2 != null)
                    {
                        int temp = recipe1.Порядок_закладки ?? 0;
                        recipe1.Порядок_закладки = recipe2.Порядок_закладки ?? 0;
                        recipe2.Порядок_закладки = temp;
                        db.SaveChanges();
                        LoadRecipes();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при перемещении: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateDishCalculations()
        {
            if (_cardId.HasValue)
            {
                CalorieCalculator.UpdateDishCalculations(_cardId.Value);
            }
        }

        private void SaveCardOnly()
        {
            try
            {
                if (DishComboBox.SelectedValue == null)
                {
                    MessageBox.Show("Выберите блюдо", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                int dishId = (int)DishComboBox.SelectedValue;
                decimal output = string.IsNullOrEmpty(OutputTextBox.Text) ? 100 : decimal.Parse(OutputTextBox.Text);

                var card = new TechnologyCardModel
                {
                    Номер = CardNumberTextBox.Text,
                    Блюдо_id = dishId,
                    Выход = output,
                    Технология_приготовления = TechnologyTextBox.Text,
                    Дата_создания = DateTime.Now,
                    Статус = ((ComboBoxItem)StatusComboBox.SelectedItem)?.Content?.ToString() ?? "Черновик"
                };

                if (_isNewCard)
                {
                    if (_techCardService.AddTechnologyCard(card))
                    {
                        using (var db = new MenuStolovayaDBEntities())
                        {
                            var newCard = db.Технологические_карты
                                .FirstOrDefault(tc => tc.Номер == card.Номер);
                            if (newCard != null)
                            {
                                _cardId = newCard.id;
                                _isNewCard = false;
                            }
                        }
                        UpdateDishCalculations();
                    }
                }
                else if (_cardId.HasValue)
                {
                    card.Id = _cardId.Value;
                    _techCardService.UpdateTechnologyCard(card);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении карты: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (DishComboBox.SelectedValue == null)
                {
                    MessageBox.Show("Выберите блюдо", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    DishComboBox.Focus();
                    return;
                }

                if (!decimal.TryParse(OutputTextBox.Text, out decimal output) || output <= 0)
                {
                    MessageBox.Show("Введите корректный выход блюда", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    OutputTextBox.Focus();
                    return;
                }

                if (string.IsNullOrWhiteSpace(TechnologyTextBox.Text))
                {
                    MessageBox.Show("Заполните технологию приготовления", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    TechnologyTextBox.Focus();
                    return;
                }

                int dishId = (int)DishComboBox.SelectedValue;

                var card = new TechnologyCardModel
                {
                    Id = _cardId ?? 0,
                    Номер = CardNumberTextBox.Text,
                    Блюдо_id = dishId,
                    Выход = output,
                    Технология_приготовления = TechnologyTextBox.Text,
                    Дата_создания = DateTime.Now,
                    Статус = ((ComboBoxItem)StatusComboBox.SelectedItem)?.Content?.ToString() ?? "Черновик"
                };

                bool success;
                if (_isNewCard)
                {
                    success = _techCardService.AddTechnologyCard(card);
                }
                else
                {
                    success = _techCardService.UpdateTechnologyCard(card);
                }

                if (success)
                {
                    if (!string.IsNullOrEmpty(CookingTimeTextBox.Text) && int.TryParse(CookingTimeTextBox.Text, out int cookingTime))
                    {
                        using (var db = new MenuStolovayaDBEntities())
                        {
                            var dish = db.Блюда.Find(dishId);
                            if (dish != null)
                            {
                                dish.Время_приготовления = cookingTime;
                                db.SaveChanges();
                            }
                        }
                    }

                    UpdateDishCalculations();

                    MessageBox.Show("Технологическая карта успешно сохранена", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);

                    DialogResult = true;
                    Close();
                }
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

        /// <summary>
        /// Печать технологической карты
        /// </summary>
        private void PrintTechCard_Click(object sender, RoutedEventArgs e)
        {
            if (!_cardId.HasValue)
            {
                MessageBox.Show("Сначала сохраните технологическую карту", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                string htmlContent = GenerateTechCardPrintHtml();
                string tempFile = Path.Combine(Path.GetTempPath(), $"TechCard_{_cardId}_{DateTime.Now:yyyyMMddHHmmss}.html");
                File.WriteAllText(tempFile, htmlContent, Encoding.UTF8);

                var psi = new ProcessStartInfo
                {
                    FileName = tempFile,
                    UseShellExecute = true,
                };
                Process.Start(psi);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при печати: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private string GenerateTechCardPrintHtml()
        {
            using (var db = new MenuStolovayaDBEntities())
            {
                var techCard = db.Технологические_карты
                    .Include("Блюда")
                    .Include("Блюда.Виды_блюд")
                    .FirstOrDefault(tc => tc.id == _cardId.Value);

                if (techCard == null) return "<html><body><h1>Карта не найдена</h1></body></html>";

                var recipes = _recipeService.GetRecipes(_cardId.Value);
                var dish = db.Блюда.Find(techCard.Блюдо_id);

                StringBuilder html = new StringBuilder();
                html.AppendLine("<!DOCTYPE html>");
                html.AppendLine("<html lang='ru'>");
                html.AppendLine("<head>");
                html.AppendLine("    <meta charset='UTF-8'>");
                html.AppendLine("    <title>Технологическая карта</title>");
                html.AppendLine("    <style>");
                html.AppendLine("        body { font-family: 'Segoe UI', Arial, sans-serif; margin: 20px; }");
                html.AppendLine("        .container { max-width: 1000px; margin: 0 auto; background: white; padding: 20px; }");
                html.AppendLine("        h1 { color: #2e7d32; border-bottom: 2px solid #4caf50; padding-bottom: 10px; }");
                html.AppendLine("        h2 { color: #333; margin-top: 20px; }");
                html.AppendLine("        .info-grid { display: grid; grid-template-columns: 150px 1fr; gap: 10px; margin: 20px 0; }");
                html.AppendLine("        .info-label { font-weight: bold; }");
                html.AppendLine("        table { width: 100%; border-collapse: collapse; margin: 15px 0; }");
                html.AppendLine("        th, td { border: 1px solid #ddd; padding: 8px; text-align: left; }");
                html.AppendLine("        th { background-color: #4caf50; color: white; }");
                html.AppendLine("        .footer { margin-top: 30px; text-align: center; font-size: 12px; color: #666; border-top: 1px solid #ddd; padding-top: 15px; }");
                html.AppendLine("        @media print { body { margin: 0; } .no-print { display: none; } }");
                html.AppendLine("    </style>");
                html.AppendLine("</head>");
                html.AppendLine("<body>");
                html.AppendLine("<div class='container'>");
                html.AppendLine($"    <h1>Технологическая карта №{techCard.Номер}</h1>");
                html.AppendLine("    <div class='info-grid'>");
                html.AppendLine($"        <div class='info-label'>Блюдо:</div><div>{techCard.Блюда?.Наименование ?? "Неизвестно"}</div>");
                html.AppendLine($"        <div class='info-label'>Вид блюда:</div><div>{techCard.Блюда?.Виды_блюд?.Наименование ?? "Не указано"}</div>");
                html.AppendLine($"        <div class='info-label'>Выход:</div><div>{techCard.Выход:N1} г</div>");
                html.AppendLine($"        <div class='info-label'>Калорийность:</div><div>{(dish?.Калорийность_расчетная ?? 0):F1} ккал/100г</div>");
                html.AppendLine($"        <div class='info-label'>Время приготовления:</div><div>{dish?.Время_приготовления ?? 30} мин</div>");
                html.AppendLine($"        <div class='info-label'>Статус:</div><div>{techCard.Статус}</div>");
                html.AppendLine($"        <div class='info-label'>Дата создания:</div><div>{techCard.Дата_создания:dd.MM.yyyy}</div>");
                html.AppendLine("    </div>");
                html.AppendLine("    <h2>Рецептура</h2>");
                html.AppendLine("    <table>");
                html.AppendLine("        <thead>");
                html.AppendLine("            <tr><th>№</th><th>Артикул</th><th>Продукт</th><th>Ед. изм.</th><th>Кол-во брутто (кг)</th><th>Потери холод.</th><th>Потери гор.</th><th>Кол-во нетто (кг)</th></tr>");
                html.AppendLine("        </thead>");
                html.AppendLine("        <tbody>");

                int index = 1;
                foreach (var recipe in recipes)
                {
                    html.AppendLine($"            <tr><td>{index++}</td><td>{recipe.Артикул}</td><td>{recipe.Продукт}</td><td>{recipe.Единица_измерения}</td><td>{recipe.Количество_брутто:F3}</td><td>{recipe.Потери_холодной:F1}%</td><td>{recipe.Потери_горячей:F1}%</td><td>{recipe.Количество_нетто:F3}</td></tr>");
                }

                html.AppendLine("        </tbody>");
                html.AppendLine("    </table>");
                html.AppendLine("    <h2>Технология приготовления</h2>");
                html.AppendLine($"    <p>{techCard.Технология_приготовления ?? "Не указана"}</p>");
                html.AppendLine("    <div class='footer'>");
                html.AppendLine($"        <div>© {DateTime.Now.Year} Меню столовой. Все права защищены.</div>");
                html.AppendLine("        <button class='no-print' onclick='window.print()'>🖨️ Распечатать</button>");
                html.AppendLine("    </div>");
                html.AppendLine("</div>");
                html.AppendLine("</body>");
                html.AppendLine("</html>");

                return html.ToString();
            }
        }

        // ИСПРАВЛЕННЫЙ МЕТОД - теперь работает с запятой
        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            foreach (char ch in e.Text)
            {
                if (!char.IsDigit(ch) && ch != '.' && ch != ',')
                {
                    e.Handled = true;
                    return;
                }
            }

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
                    MessageBox.Show("Можно ввести только одну десятичную точку или запятую",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
        }
    }
}