using MenuStolovaya.Models;
using MenuStolovaya.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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

namespace MenuStolovaya.Views
{
    public partial class EditRecipeWindow : Window
    {
        private int _technologyCardId;
        private RecipeService _recipeService;
        private TechnologyCardService _techCardService;

        public EditRecipeWindow(int technologyCardId)
        {
            InitializeComponent();
            _technologyCardId = technologyCardId;
            _recipeService = new RecipeService();
            _techCardService = new TechnologyCardService();

            LoadData();
        }

        private void LoadData()
        {
            try
            {
                // Загружаем информацию о технологической карте
                var techCard = _techCardService.GetTechnologyCardById(_technologyCardId);
                if (techCard != null)
                {
                    // ПРАВИЛЬНО: показываем в килограммах
                    decimal outputInKg = techCard.Выход / 1000;
                    TechCardInfoText.Text = $"Технологическая карта: {techCard.Номер} (Выход: {outputInKg:F3} кг)";
                }

                // Загружаем продукты для выпадающего списка
                using (var db = new MenuStolovayaDBEntities())
                {
                    var products = db.Продукты
                        .Where(p => p.Активен == true)
                        .Select(p => new
                        {
                            Id = p.id,
                            Наименование = p.Наименование,
                            Единица = p.Единица_измерения,
                            Калорийность = p.Калорийность // Добавляем калорийность для отображения
                        })
                        .ToList();

                    // Отображаем продукты с калорийностью
                    ProductComboBox.ItemsSource = products.Select(p => new
                    {
                        p.Id,
                        DisplayText = $"{p.Наименование} ({p.Калорийность ?? 0} кал/100г)"
                    }).ToList();

                    ProductComboBox.DisplayMemberPath = "DisplayText";
                    ProductComboBox.SelectedValuePath = "Id";

                    // Устанавливаем следующий порядковый номер
                    var maxOrder = db.Рецептуры
                        .Where(r => r.Технологическая_карта_id == _technologyCardId)
                        .Max(r => (int?)r.Порядок_закладки) ?? 0;

                    OrderTextBox.Text = (maxOrder + 1).ToString();
                }

                // Загружаем рецептуру
                LoadRecipe();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке данных: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadRecipe()
        {
            var recipes = _recipeService.GetRecipes(_technologyCardId);
            RecipeDataGrid.ItemsSource = recipes;

            // Показываем итоговую информацию
            UpdateSummaryInfo();
        }

        private void UpdateSummaryInfo()
        {
            try
            {
                using (var db = new MenuStolovayaDBEntities())
                {
                    var techCard = db.Технологические_карты.Find(_technologyCardId);
                    if (techCard != null)
                    {
                        var dish = db.Блюда.Find(techCard.Блюдо_id);
                        if (dish != null)
                        {
                            // ИСПРАВЛЕНО: Калорийность_расчетная уже в ккал/100г
                            decimal caloriesPer100g = dish.Калорийность_расчетная ?? 0;

                            TechCardInfoText.Text = $"Технологическая карта: {techCard.Номер} | " +
                                                  $"Выход: {techCard.Выход} г | " +
                                                  $"Калорийность: {caloriesPer100g:F1} ккал/100г";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка при обновлении информации: {ex.Message}");
            }
        }

        private void AddProductButton_Click(object sender, RoutedEventArgs e)
        {
            if (ProductComboBox.SelectedItem == null)
            {
                MessageBox.Show("Выберите продукт", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!decimal.TryParse(QuantityTextBox.Text, out decimal quantity) || quantity <= 0)
            {
                MessageBox.Show("Введите корректное количество\n\n" +
                               "ВАЖНО: Указывайте количество в КИЛОГРАММАХ!\n" +
                               "Например:\n" +
                               "• 10 шт яблок ≈ 1 кг\n" +
                               "• 0.5 л молока ≈ 0.5 кг\n" +
                               "• 250 г муки = 0.25 кг",
                    "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                QuantityTextBox.Focus();
                return;
            }

            if (!int.TryParse(OrderTextBox.Text, out int order) || order <= 0)
            {
                MessageBox.Show("Введите корректный порядковый номер", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                OrderTextBox.Focus();
                return;
            }

            try
            {
                var selectedItem = ProductComboBox.SelectedItem as dynamic;
                int productId = selectedItem.Id;

                var recipe = new RecipeModel
                {
                    Технологическая_карта_id = _technologyCardId,
                    Продукт_id = productId,
                    Количество_брутто = quantity,
                    Порядок_закладки = order
                };

                if (_recipeService.AddRecipe(recipe))
                {
                    LoadRecipe();
                    ProductComboBox.SelectedIndex = -1;
                    QuantityTextBox.Text = "0";

                    // Увеличиваем порядковый номер для следующего продукта
                    OrderTextBox.Text = (order + 1).ToString();

                    // ОБНОВЛЯЕМ ИНФОРМАЦИЮ СРАЗУ ПОСЛЕ ДОБАВЛЕНИЯ
                    UpdateSummaryInfo();

                    MessageBox.Show("Продукт добавлен в рецептуру.\n" +
                                  "Выход и калорийность блюда автоматически пересчитаны.",
                                  "Успех",
                                  MessageBoxButton.OK,
                                  MessageBoxImage.Information);
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
            var selectedRecipe = RecipeDataGrid.SelectedItem as RecipeDisplay;
            if (selectedRecipe != null)
            {
                // Здесь можно открыть окно редактирования отдельной строки рецептуры
                // Пока просто сделаем удаление и добавление заново
                var result = MessageBox.Show("Редактирование через удаление/добавление. Удалить эту строку?",
                    "Редактирование", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    if (_recipeService.DeleteRecipe(selectedRecipe.Id))
                    {
                        LoadRecipe();
                        _recipeService.UpdateRecipeOrder(_technologyCardId);
                    }
                }
            }
        }

        private void DeleteRecipeButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedRecipe = RecipeDataGrid.SelectedItem as RecipeDisplay;
            if (selectedRecipe != null)
            {
                var result = MessageBox.Show($"Удалить продукт \"{selectedRecipe.Продукт}\" из рецептуры?",
                    "Подтверждение удаления", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    if (_recipeService.DeleteRecipe(selectedRecipe.Id))
                    {
                        LoadRecipe();
                        _recipeService.UpdateRecipeOrder(_technologyCardId);

                        // ОБНОВЛЯЕМ ИНФОРМАЦИЮ ПОСЛЕ УДАЛЕНИЯ
                        UpdateSummaryInfo();
                    }
                }
            }
        }

        private void MoveUpButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedRecipe = RecipeDataGrid.SelectedItem as RecipeDisplay;
            if (selectedRecipe != null && selectedRecipe.Порядок_закладки > 1)
            {
                try
                {
                    using (var db = new MenuStolovayaDBEntities())
                    {
                        // Находим продукт выше
                        var recipeAbove = db.Рецептуры
                            .FirstOrDefault(r => r.Технологическая_карта_id == _technologyCardId &&
                                               r.Порядок_закладки == selectedRecipe.Порядок_закладки - 1);

                        var currentRecipe = db.Рецептуры.Find(selectedRecipe.Id);

                        if (recipeAbove != null && currentRecipe != null)
                        {
                            // Меняем местами порядковые номера
                            int temp = recipeAbove.Порядок_закладки ?? 0;
                            recipeAbove.Порядок_закладки = currentRecipe.Порядок_закладки ?? 0;
                            currentRecipe.Порядок_закладки = temp;

                            db.SaveChanges();
                            LoadRecipe();
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
            var selectedRecipe = RecipeDataGrid.SelectedItem as RecipeDisplay;
            if (selectedRecipe != null)
            {
                try
                {
                    using (var db = new MenuStolovayaDBEntities())
                    {
                        // Находим продукт ниже
                        var recipeBelow = db.Рецептуры
                            .FirstOrDefault(r => r.Технологическая_карта_id == _technologyCardId &&
                                               r.Порядок_закладки == selectedRecipe.Порядок_закладки + 1);

                        var currentRecipe = db.Рецептуры.Find(selectedRecipe.Id);

                        if (recipeBelow != null && currentRecipe != null)
                        {
                            // Меняем местами порядковые номера
                            int temp = recipeBelow.Порядок_закладки ?? 0;
                            recipeBelow.Порядок_закладки = currentRecipe.Порядок_закладки ?? 0;
                            currentRecipe.Порядок_закладки = temp;

                            db.SaveChanges();
                            LoadRecipe();
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
            try
            {
                using (var db = new MenuStolovayaDBEntities())
                {
                    var techCard = db.Технологические_карты.Find(_technologyCardId);
                    if (techCard != null)
                    {
                        var dish = db.Блюда.Find(techCard.Блюдо_id);
                        if (dish != null)
                        {
                            CalorieCalculator.UpdateDishCalculations(_technologyCardId);
                            db.Entry(dish).Reload();

                            // ИСПРАВЛЕНО: Калорийность_расчетная уже в ккал/100г
                            decimal caloriesPer100g = dish.Калорийность_расчетная ?? 0;
                            decimal totalCaloriesInKcal = CalorieCalculator.CalculateTotalDishCaloriesInKcal(dish.id);

                            string verification = $"=== РАСЧЕТЫ БЛЮДА '{dish.Наименование}' ===\n" +
                                                $"Выход: {dish.Выход_стандартный:F0} г\n" +
                                                $"Калорийность на 100г: {caloriesPer100g:F1} ккал/100г\n" +
                                                $"Общая калорийность порции: {totalCaloriesInKcal:F2} ккал\n" +
                                                $"\nРецептура сохранена. Все расчеты выполнены автоматически.";

                            MessageBox.Show(verification, "Сохранение завершено",
                                MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка при проверке: {ex.Message}");
            }

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