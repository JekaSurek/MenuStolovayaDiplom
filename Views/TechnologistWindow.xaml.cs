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
using MenuStolovaya.Views;

namespace MenuStolovaya.Views
{

    public partial class TechnologistWindow : Window
    {
        private Dictionary<Button, Style> _buttonStyles = new Dictionary<Button, Style>();
        private ProductService _productService = new ProductService();
        private CategoryService _categoryService = new CategoryService();
        private DishService _dishService = new DishService();
        private DishTypeService _dishTypeService = new DishTypeService();
        private TechnologyCardService _techCardService = new TechnologyCardService();
        private MenuService _menuService = new MenuService();
        private MenuPrinterService _menuPrinterService = new MenuPrinterService();
        private bool _isMaximized = false;

        public TechnologistWindow()
        {
            InitializeComponent();

            // Сохраняем обычные стили кнопок
            _buttonStyles[ProductsTabButton] = ProductsTabButton.Style;
            _buttonStyles[CategoriesTabButton] = CategoriesTabButton.Style;
            _buttonStyles[DishesTabButton] = DishesTabButton.Style;
            _buttonStyles[DishTypesTabButton] = DishTypesTabButton.Style;
            _buttonStyles[TechnologyCardsTabButton] = TechnologyCardsTabButton.Style;
            _buttonStyles[MenusTabButton] = MenusTabButton.Style;
            _buttonStyles[HelpTabButton] = HelpTabButton.Style;

            LoadCurrentUserInfo();
            LoadProducts();

            // Устанавливаем активную вкладку после загрузки
            this.Loaded += (s, e) => ShowTab(ProductsContent, ProductsTabButton);
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void MaximizeButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isMaximized)
            {
                this.WindowState = WindowState.Normal;
                _isMaximized = false;
                MaximizeButton.Content = "□";
                MaximizeButton.ToolTip = "Развернуть";
            }
            else
            {
                this.WindowState = WindowState.Maximized;
                _isMaximized = true;
                MaximizeButton.Content = "❐";
                MaximizeButton.ToolTip = "Восстановить";
            }
        }

        // Если окно сворачивается/разворачивается другими способами, добавьте обработчик
        protected override void OnStateChanged(EventArgs e)
        {
            base.OnStateChanged(e);

            if (this.WindowState == WindowState.Maximized)
            {
                _isMaximized = true;
                MaximizeButton.Content = "❐";
                MaximizeButton.ToolTip = "Восстановить";
            }
            else if (this.WindowState == WindowState.Normal)
            {
                _isMaximized = false;
                MaximizeButton.Content = "□";
                MaximizeButton.ToolTip = "Развернуть";
            }
        }

        private void LoadCurrentUserInfo()
        {
            CurrentUserText.Text = ThisUser.CurrentUser?.FullName ?? "Неизвестно";
        }

        #region Продукты
        private void LoadProducts()
        {
            try
            {
                var products = _productService.GetProducts();
                ProductsDataGrid.ItemsSource = products;

                // Активируем/деактивируем кнопки
                ProductsDataGrid_SelectionChanged(null, null);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке продуктов: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AddProductButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var addWindow = new AddEditProductWindow();
                if (addWindow.ShowDialog() == true)
                {
                    var product = addWindow.Product;
                    if (_productService.AddProduct(product))
                    {
                        MessageBox.Show("Продукт успешно добавлен", "Успех",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                        LoadProducts();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при добавлении продукта: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void EditProductButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedProduct = ProductsDataGrid.SelectedItem as ProductDisplay;
            if (selectedProduct != null)
            {
                try
                {
                    using (var db = new MenuStolovayaDBEntities())
                    {
                        var product = db.Продукты.Find(selectedProduct.Id);
                        if (product != null && product.Активен == true)
                        {
                            var editWindow = new AddEditProductWindow(product);
                            if (editWindow.ShowDialog() == true)
                            {
                                var updatedProduct = editWindow.Product;
                                if (_productService.UpdateProduct(updatedProduct))
                                {
                                    MessageBox.Show("Продукт успешно обновлен", "Успех",
                                        MessageBoxButton.OK, MessageBoxImage.Information);
                                    LoadProducts();
                                }
                            }
                        }
                        else
                        {
                            MessageBox.Show("Продукт не найден или удален", "Ошибка",
                                MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при редактировании продукта: {ex.Message}",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                MessageBox.Show("Выберите продукт для редактирования", "Внимание",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void DeleteProductButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedProduct = ProductsDataGrid.SelectedItem as ProductDisplay;
            if (selectedProduct != null)
            {
                var result = MessageBox.Show($"Удалить продукт \"{selectedProduct.Наименование}\"?\n\nПродукт будет помечен как неактивный и скрыт из списка.",
                    "Подтверждение удаления", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    if (_productService.DeleteProduct(selectedProduct.Id))
                    {
                        MessageBox.Show("Продукт успешно удален (помечен как неактивный)", "Успех",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                        LoadProducts();
                    }
                }
            }
            else
            {
                MessageBox.Show("Выберите продукт для удаления", "Внимание",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void RefreshProductsButton_Click(object sender, RoutedEventArgs e)
        {
            LoadProducts();
        }

        private void ProductSearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                var filter = ProductSearchBox.Text;
                var products = _productService.GetProducts(filter);
                ProductsDataGrid.ItemsSource = products;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при поиске: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ProductsDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var hasSelection = ProductsDataGrid.SelectedItem != null;
            EditProductButton.IsEnabled = hasSelection;
            DeleteProductButton.IsEnabled = hasSelection;
        }
        #endregion

        #region Категории

        private void LoadCategories()
        {
            try
            {
                var categories = _categoryService.GetCategories(CategorySearchBox.Text);
                CategoriesDataGrid.ItemsSource = categories;
                UpdateCategoryButtonsState();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке категорий: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateCategoryButtonsState()
        {
            bool hasSelection = CategoriesDataGrid.SelectedItem != null;
            EditCategoryButton.IsEnabled = hasSelection;
            DeleteCategoryButton.IsEnabled = hasSelection;
        }

        private void AddCategoryButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var addWindow = new AddEditCategoryWindow();
                if (addWindow.ShowDialog() == true)
                {
                    var category = addWindow.Category;
                    if (_categoryService.AddCategory(category))
                    {
                        MessageBox.Show("Категория успешно добавлена", "Успех",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                        LoadCategories();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при добавлении категории: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void EditCategoryButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedCategory = CategoriesDataGrid.SelectedItem as CategoryModel;
            if (selectedCategory != null)
            {
                try
                {
                    using (var db = new MenuStolovayaDBEntities())
                    {
                        var category = db.Категории_продуктов.Find(selectedCategory.Id);
                        if (category != null)
                        {
                            var editWindow = new AddEditCategoryWindow(category);
                            if (editWindow.ShowDialog() == true)
                            {
                                var updatedCategory = editWindow.Category;
                                if (_categoryService.UpdateCategory(updatedCategory))
                                {
                                    MessageBox.Show("Категория успешно обновлена", "Успех",
                                        MessageBoxButton.OK, MessageBoxImage.Information);
                                    LoadCategories();
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при редактировании категории: {ex.Message}",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                MessageBox.Show("Выберите категорию для редактирования", "Внимание",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void DeleteCategoryButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedCategory = CategoriesDataGrid.SelectedItem as CategoryModel;
            if (selectedCategory != null)
            {
                var result = MessageBox.Show($"Удалить категорию \"{selectedCategory.Наименование}\"?\n\nЭто действие невозможно отменить.",
                    "Подтверждение удаления", MessageBoxButton.YesNo, MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    if (_categoryService.DeleteCategory(selectedCategory.Id))
                    {
                        MessageBox.Show("Категория успешно удалена", "Успех",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                        LoadCategories();
                    }
                }
            }
            else
            {
                MessageBox.Show("Выберите категорию для удаления", "Внимание",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void CategorySearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            LoadCategories();
        }

        private void CategoriesDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateCategoryButtonsState();
        }

        #endregion

        #region Блюда
        private void LoadDishes()
        {
            try
            {
                var dishes = _dishService.GetDishes();
                DishesDataGrid.ItemsSource = dishes;

                // Активируем/деактивируем кнопки
                DishesDataGrid_SelectionChanged(null, null);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке блюд: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AddDishButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var addWindow = new AddEditDishWindow();
                if (addWindow.ShowDialog() == true)
                {
                    var dish = addWindow.Dish;
                    if (_dishService.AddDish(dish))
                    {
                        MessageBox.Show("Блюдо успешно добавлено", "Успех",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                        LoadDishes();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при добавлении блюда: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void EditDishButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedDish = DishesDataGrid.SelectedItem as DishDisplay;
            if (selectedDish != null)
            {
                try
                {
                    using (var db = new MenuStolovayaDBEntities())
                    {
                        var dish = db.Блюда.Find(selectedDish.Id);
                        if (dish != null && dish.Активно == true)
                        {
                            var editWindow = new AddEditDishWindow(dish);
                            if (editWindow.ShowDialog() == true)
                            {
                                var updatedDish = editWindow.Dish;
                                if (_dishService.UpdateDish(updatedDish))
                                {
                                    MessageBox.Show("Блюдо успешно обновлено", "Успех",
                                        MessageBoxButton.OK, MessageBoxImage.Information);
                                    LoadDishes();
                                }
                            }
                        }
                        else
                        {
                            MessageBox.Show("Блюдо не найдено или удалено", "Ошибка",
                                MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при редактировании блюда: {ex.Message}",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                MessageBox.Show("Выберите блюдо для редактирования", "Внимание",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void DeleteDishButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedDish = DishesDataGrid.SelectedItem as DishDisplay;
            if (selectedDish != null)
            {
                var result = MessageBox.Show($"Удалить блюдо \"{selectedDish.Наименование}\"?\n\nБлюдо будет помечено как неактивное и скрыто из списка.",
                    "Подтверждение удаления", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    if (_dishService.DeleteDish(selectedDish.Id))
                    {
                        MessageBox.Show("Блюдо успешно удалено (помечено как неактивное)", "Успех",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                        LoadDishes();
                    }
                }
            }
            else
            {
                MessageBox.Show("Выберите блюдо для удаления", "Внимание",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void UpdateCaloriesButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var result = MessageBox.Show("Пересчитать калорийность для всех блюд?\n\nЭто может занять некоторое время.",
                    "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    CalorieCalculator.UpdateAllDishesCalories();
                    MessageBox.Show("Калорийность всех блюд обновлена (в ккал/100г)", "Успех",  // ИСПРАВЛЕНО
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    LoadDishes();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при обновлении калорийности: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DishSearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                var filter = DishSearchBox.Text;
                var dishes = _dishService.GetDishes(filter);
                DishesDataGrid.ItemsSource = dishes;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при поиске: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DishesDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var hasSelection = DishesDataGrid.SelectedItem != null;
            EditDishButton.IsEnabled = hasSelection;
            DeleteDishButton.IsEnabled = hasSelection;
        }
        #endregion

        #region Виды блюд

        private void LoadDishTypes()
        {
            try
            {
                var dishTypes = _dishTypeService.GetDishTypes(DishTypeSearchBox.Text);
                DishTypesDataGrid.ItemsSource = dishTypes;
                UpdateDishTypeButtonsState();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке видов блюд: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateDishTypeButtonsState()
        {
            bool hasSelection = DishTypesDataGrid.SelectedItem != null;
            EditDishTypeButton.IsEnabled = hasSelection;
            DeleteDishTypeButton.IsEnabled = hasSelection;
        }

        private void AddDishTypeButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var addWindow = new AddEditDishTypeWindow();
                if (addWindow.ShowDialog() == true)
                {
                    var dishType = addWindow.DishType;
                    if (_dishTypeService.AddDishType(dishType))
                    {
                        MessageBox.Show("Вид блюда успешно добавлен", "Успех",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                        LoadDishTypes();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при добавлении вида блюда: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void EditDishTypeButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedDishType = DishTypesDataGrid.SelectedItem as DishTypeModel;
            if (selectedDishType != null)
            {
                try
                {
                    using (var db = new MenuStolovayaDBEntities())
                    {
                        var dishType = db.Виды_блюд.Find(selectedDishType.Id);
                        if (dishType != null)
                        {
                            var editWindow = new AddEditDishTypeWindow(dishType);
                            if (editWindow.ShowDialog() == true)
                            {
                                var updatedDishType = editWindow.DishType;
                                if (_dishTypeService.UpdateDishType(updatedDishType))
                                {
                                    MessageBox.Show("Вид блюда успешно обновлен", "Успех",
                                        MessageBoxButton.OK, MessageBoxImage.Information);
                                    LoadDishTypes();
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при редактировании вида блюда: {ex.Message}",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                MessageBox.Show("Выберите вид блюда для редактирования", "Внимание",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void DeleteDishTypeButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedDishType = DishTypesDataGrid.SelectedItem as DishTypeModel;
            if (selectedDishType != null)
            {
                var result = MessageBox.Show($"Удалить вид блюда \"{selectedDishType.Наименование}\"?\n\nЭто действие невозможно отменить.",
                    "Подтверждение удаления", MessageBoxButton.YesNo, MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    if (_dishTypeService.DeleteDishType(selectedDishType.Id))
                    {
                        MessageBox.Show("Вид блюда успешно удален", "Успех",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                        LoadDishTypes();
                    }
                }
            }
            else
            {
                MessageBox.Show("Выберите вид блюда для удаления", "Внимание",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void DishTypeSearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            LoadDishTypes();
        }

        private void DishTypesDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateDishTypeButtonsState();
        }

        #endregion

        #region Технологические карты
        private void LoadTechnologyCards()
        {
            try
            {
                var cards = _techCardService.GetTechnologyCards();
                TechnologyCardsDataGrid.ItemsSource = cards;
                TechnologyCardsDataGrid_SelectionChanged(null, null);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке технологических карт: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AddTechnologyCardButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var editor = new TechnologyCardEditorWindow();
                if (editor.ShowDialog() == true)
                {
                    LoadTechnologyCards();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при создании технологической карты: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void EditTechnologyCardButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedCard = TechnologyCardsDataGrid.SelectedItem as TechnologyCardDisplay;
            if (selectedCard != null)
            {
                try
                {
                    var editor = new TechnologyCardEditorWindow(selectedCard.Id);
                    if (editor.ShowDialog() == true)
                    {
                        LoadTechnologyCards();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при редактировании технологической карты: {ex.Message}",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                MessageBox.Show("Выберите технологическую карту для редактирования", "Внимание",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void DeleteTechnologyCardButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedCard = TechnologyCardsDataGrid.SelectedItem as TechnologyCardDisplay;
            if (selectedCard != null)
            {
                var result = MessageBox.Show($"Удалить технологическую карту \"{selectedCard.Номер}\"?\n\nЭто действие невозможно отменить.",
                    "Подтверждение удаления", MessageBoxButton.YesNo, MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    if (_techCardService.DeleteTechnologyCard(selectedCard.Id))
                    {
                        MessageBox.Show("Технологическая карта успешно удалена", "Успех",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                        LoadTechnologyCards();
                    }
                }
            }
            else
            {
                MessageBox.Show("Выберите технологическую карту для удаления", "Внимание",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        /*private void EditRecipeButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedCard = TechnologyCardsDataGrid.SelectedItem as TechnologyCardDisplay;
            if (selectedCard != null)
            {
                try
                {
                    var editWindow = new EditRecipeWindow(selectedCard.Id);
                    editWindow.ShowDialog();
                    LoadTechnologyCards();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при открытии редактора рецептуры: {ex.Message}",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                MessageBox.Show("Выберите технологическую карту для редактирования рецептуры",
                    "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        } 
        */

        private void RefreshTechnologyCardsButton_Click(object sender, RoutedEventArgs e)
        {
            LoadTechnologyCards();
        }

        private void TechnologyCardSearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                var filter = TechnologyCardSearchBox.Text;
                var cards = _techCardService.GetTechnologyCards(filter);
                TechnologyCardsDataGrid.ItemsSource = cards;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при поиске: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void TechnologyCardsDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var hasSelection = TechnologyCardsDataGrid.SelectedItem != null;
            EditTechnologyCardButton.IsEnabled = hasSelection;
            DeleteTechnologyCardButton.IsEnabled = hasSelection;
            //EditRecipeButton.IsEnabled = hasSelection;
        }
        #endregion

        #region Меню
        private void LoadMenus()
        {
            try
            {
                var menus = _menuService.GetDailyMenus();

                // Просто отображаем как есть, без дополнительных преобразований
                MenusDataGrid.ItemsSource = menus;
                MenusDataGrid_SelectionChanged(null, null);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке меню: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AddMenuButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var editor = new MenuEditorWindow();
                if (editor.ShowDialog() == true)
                {
                    LoadMenus();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при создании меню: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void EditMenuButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedMenu = MenusDataGrid.SelectedItem as DailyMenuDisplay;
            if (selectedMenu != null)
            {
                try
                {
                    var editor = new MenuEditorWindow(selectedMenu.Id);
                    if (editor.ShowDialog() == true)
                    {
                        LoadMenus();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при редактировании меню: {ex.Message}",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                MessageBox.Show("Выберите меню для редактирования", "Внимание",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void DeleteMenuButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedMenu = MenusDataGrid.SelectedItem as DailyMenuDisplay;
            if (selectedMenu != null)
            {
                var result = MessageBox.Show($"Удалить меню на дату {selectedMenu.Дата:dd.MM.yyyy}?\n\nЭто действие невозможно отменить.",
                    "Подтверждение удаления", MessageBoxButton.YesNo, MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    if (_menuService.DeleteDailyMenu(selectedMenu.Id))
                    {
                        MessageBox.Show("Меню успешно удалено", "Успех",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                        LoadMenus();
                    }
                }
            }
            else
            {
                MessageBox.Show("Выберите меню для удаления", "Внимание",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

       /* private void EditMenuItemsButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedMenu = MenusDataGrid.SelectedItem as DailyMenuDisplay;
            if (selectedMenu != null)
            {
                try
                {
                    var editWindow = new EditMenuItemsWindow(selectedMenu.Id);
                    if (editWindow.ShowDialog() == true)
                    {
                        MessageBox.Show("Состав меню успешно обновлен", "Успех",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                        LoadMenus();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при редактировании состава меню: {ex.Message}",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                MessageBox.Show("Выберите меню для редактирования состава",
                    "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        } */

        private void RefreshMenusButton_Click(object sender, RoutedEventArgs e)
        {
            LoadMenus();
        }

        private void MenuSearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                var filter = MenuSearchBox.Text;
                var menus = _menuService.GetDailyMenus(filter);
                MenusDataGrid.ItemsSource = menus;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при поиске: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void MenusDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var hasSelection = MenusDataGrid.SelectedItem != null;
            EditMenuButton.IsEnabled = hasSelection;
            DeleteMenuButton.IsEnabled = hasSelection;
            //EditMenuItemsButton.IsEnabled = hasSelection;
            
        }
        #endregion

        #region Навигация по вкладкам
        private void ProductsTabButton_Click(object sender, RoutedEventArgs e)
        {
            ShowTab(ProductsContent, ProductsTabButton);
            LoadProducts();
        }

        private void CategoriesTabButton_Click(object sender, RoutedEventArgs e)
        {
            ShowTab(CategoriesContent, CategoriesTabButton);
            LoadCategories();
        }

        private void DishesTabButton_Click(object sender, RoutedEventArgs e)
        {
            ShowTab(DishesContent, DishesTabButton);
            LoadDishes();
        }

        private void DishTypesTabButton_Click(object sender, RoutedEventArgs e)
        {
            ShowTab(DishTypesContent, DishTypesTabButton);
            LoadDishTypes();
        }

        private void TechnologyCardsTabButton_Click(object sender, RoutedEventArgs e)
        {
            ShowTab(TechnologyCardsContent, TechnologyCardsTabButton);
            LoadTechnologyCards();
        }

        private void MenusTabButton_Click(object sender, RoutedEventArgs e)
        {
            ShowTab(MenusContent, MenusTabButton);
            LoadMenus();
        }

        private void HelpTabButton_Click(object sender, RoutedEventArgs e)
        {
            ShowTab(HelpContent, HelpTabButton);
        }

        private void ShowTab(Grid tabContent, Button activeButton = null)
        {
            ProductsContent.Visibility = Visibility.Collapsed;
            CategoriesContent.Visibility = Visibility.Collapsed;
            DishesContent.Visibility = Visibility.Collapsed;
            DishTypesContent.Visibility = Visibility.Collapsed;
            TechnologyCardsContent.Visibility = Visibility.Collapsed;
            MenusContent.Visibility = Visibility.Collapsed;
            HelpContent.Visibility = Visibility.Collapsed;

            tabContent.Visibility = Visibility.Visible;

            // Сбрасываем стили всех кнопок
            foreach (var btn in _buttonStyles.Keys)
            {
                btn.Style = _buttonStyles[btn];
            }

            // Устанавливаем активный стиль для выбранной кнопки
            if (activeButton != null)
            {
                var activeStyle = new Style(typeof(Button));
                activeStyle.Setters.Add(new Setter(Button.BackgroundProperty, new SolidColorBrush(Color.FromRgb(76, 175, 80))));
                activeStyle.Setters.Add(new Setter(Button.ForegroundProperty, Brushes.White));
                activeStyle.Setters.Add(new Setter(BorderThicknessProperty, new Thickness(0)));
                activeStyle.Setters.Add(new Setter(HeightProperty, 50.0));
                activeStyle.Setters.Add(new Setter(HorizontalContentAlignmentProperty, HorizontalAlignment.Left));
                activeStyle.Setters.Add(new Setter(PaddingProperty, new Thickness(20, 0, 0, 0)));
                activeStyle.Setters.Add(new Setter(FontSizeProperty, 14.0));
                activeStyle.Setters.Add(new Setter(FontWeightProperty, FontWeights.SemiBold));
                activeStyle.Setters.Add(new Setter(CursorProperty, Cursors.Hand));

                var template = new ControlTemplate(typeof(Button));
                var border = new FrameworkElementFactory(typeof(Border));
                border.SetValue(Border.CornerRadiusProperty, new CornerRadius(0, 10, 10, 0));
                border.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Button.BackgroundProperty));
                var content = new FrameworkElementFactory(typeof(ContentPresenter));
                content.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Left);
                content.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
                content.SetValue(ContentPresenter.MarginProperty, new Thickness(20, 0, 0, 0));
                border.AppendChild(content);
                template.VisualTree = border;
                activeStyle.Setters.Add(new Setter(Button.TemplateProperty, template));

                activeButton.Style = activeStyle;
            }
        }
        #endregion

        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            ThisUser.ClearCurrentUser();
            var loginWindow = new LoginWindow();
            loginWindow.Show();
            this.Close();
        }
        private void PrintMenuButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedMenu = MenusDataGrid.SelectedItem as DailyMenuDisplay;
            if (selectedMenu != null)
            {
                try
                {
                    // Создаем диалог сохранения файла
                    var saveDialog = new Microsoft.Win32.SaveFileDialog
                    {
                        Filter = "HTML файлы (*.html)|*.html|Все файлы (*.*)|*.*",
                        FileName = $"Меню_{selectedMenu.Дата:yyyyMMdd}.html",
                        DefaultExt = ".html",
                        AddExtension = true
                    };

                    if (saveDialog.ShowDialog() == true)
                    {
                        // Генерируем и сохраняем HTML
                        _menuPrinterService.SaveHtmlToFile(selectedMenu.Id, saveDialog.FileName);

                        // Открываем в браузере
                        _menuPrinterService.ShowMenuInBrowser(selectedMenu.Id);

                        MessageBox.Show($"Меню сохранено и открыто в браузере:\n{saveDialog.FileName}",
                            "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при печати меню: {ex.Message}",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

       
        private void CreateRequestButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedMenu = MenusDataGrid.SelectedItem as DailyMenuDisplay;
            if (selectedMenu != null)
            {
                try
                {
                    using (var db = new MenuStolovayaDBEntities())
                    {
                        // Проверяем, нет ли уже требования для этого меню
                        var existingRequest = db.Требования_накладные
                            .FirstOrDefault(tr => tr.Меню_id == selectedMenu.Id &&
                                                 tr.Статус_требования == "Ожидает");

                        if (existingRequest != null)
                        {
                            MessageBox.Show("Для этого меню уже создано требование накладная, ожидающее обработки",
                                "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }

                        // Генерация номера требования
                        string requestNumber = $"ТР-{DateTime.Now:yyyyMMdd}-{new Random().Next(1000, 9999)}";

                        // Создаем документ требование
                        var document = new Документы
                        {
                            Номер = requestNumber,
                            Тип_документа = "Требование",
                            Дата_документа = DateTime.Now,
                            Склад_отправитель_id = 1, // Основной склад
                            Статус = "Черновик",
                            Кто_создал_id = ThisUser.CurrentUser.Id,
                            Комментарий = $"Требование на меню от {selectedMenu.Дата:dd.MM.yyyy}"
                        };
                        db.Документы.Add(document);
                        db.SaveChanges();

                        // Добавляем строки требования на основе меню
                        var menuItems = db.Строки_меню
                            .Where(sm => sm.Меню_id == selectedMenu.Id)
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
                                        // Рассчитываем количество нетто с учетом потерь
                                        decimal netto = (recipe.Количество_нетто ?? recipe.Количество_брутто) * (item.Количество_порций ?? 1);

                                        var existingLine = db.Строки_документов
                                            .FirstOrDefault(sd => sd.Документ_id == document.id &&
                                                                 sd.Продукт_id == recipe.Продукт_id);

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
                                                Цена = product.Цена ?? 0 // Исправлено: используем ?? 0 для decimal?
                                            });
                                        }
                                    }
                                }
                            }
                        }

                        db.SaveChanges();

                        // Создаем запись требования
                        var request = new Требования_накладные
                        {
                            Номер = requestNumber,
                            Документ_id = document.id,
                            Меню_id = selectedMenu.Id,
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
        }

        private void HelpButton_Click(object sender, RoutedEventArgs e)
        {
            string helpPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Help", "help.html");

            if (System.IO.File.Exists(helpPath))
            {
                System.Diagnostics.Process.Start(helpPath);
            }
            else
            {
                MessageBox.Show("Файл справки не найден!\n\n" +
                               "Ожидаемый путь: " + helpPath,
                               "Ошибка",
                               MessageBoxButton.OK,
                               MessageBoxImage.Error);
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Вы уверены, что хотите выйти из программы?",
                "Подтверждение выхода",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                Application.Current.Shutdown();
            }
        }
    }
}