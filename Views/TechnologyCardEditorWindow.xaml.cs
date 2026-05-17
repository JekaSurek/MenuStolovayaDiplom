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
                            // Калорийность в БД в калориях, делим на 1000 для перевода в килокалории на 100г
                            decimal caloriesPer100g = dish.Калорийность_расчетная.Value / 1000;
                            CaloriesText.Text = $"{caloriesPer100g:F2} ккал / 100г";
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

            decimal totalNettoGrams = _currentRecipes.Sum(r => r.Количество_нетто * 1000); // Переводим кг в г
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
                var result = MessageBox.Show("Сначала нужно сохранить технологическую карту. Сохранить сейчас?",
                    "Сохранение карты", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    SaveCardOnly();
                    if (!_cardId.HasValue) return;
                }
                else
                {
                    return;
                }
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

                    // Пересчитываем выход и калорийность
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
                        // Получаем ID новой карты
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
                    // Обновляем время приготовления в блюде
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

        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex(@"^[0-9]*(?:\.[0-9]*)?$");
            e.Handled = !regex.IsMatch(e.Text);
        }
    }
}