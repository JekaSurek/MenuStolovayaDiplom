using MenuStolovaya.Models;
using MenuStolovaya.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace MenuStolovaya.Views
{
    public partial class StorekeeperWindow : Window
    {
        private List<RequestDisplay> _requests;
        private List<StockDisplay> _stocks;
        private List<DocumentDisplay> _incomes;
        private List<DocumentDisplay> _expenses;
        private ProductService _productService;
        private List<ProductDisplay> _storekeeperProducts;

        public StorekeeperWindow()
        {
            InitializeComponent();
            _productService = new ProductService();
            LoadCurrentUserInfo();
            LoadData();
            LoadWarehouses();
        }

        private void LoadCurrentUserInfo()
        {
            CurrentUserText.Text = ThisUser.CurrentUser?.FullName ?? "Неизвестно";
        }

        private void LoadWarehouses()
        {
            try
            {
                using (var db = new MenuStolovayaDBEntities())
                {
                    var warehouses = db.Склады
                        .Where(s => s.Активен == true)
                        .Select(s => new { Id = s.id, Name = s.Наименование })
                        .ToList();

                    WarehouseFilter.ItemsSource = warehouses;
                    WarehouseFilter.DisplayMemberPath = "Name";
                    WarehouseFilter.SelectedValuePath = "Id";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки складов: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadData()
        {
            LoadRequests();
            LoadStocks();
            LoadDocuments();
            LoadStorekeeperProducts();
        }

        #region Требования накладные

        private void LoadRequests()
        {
            try
            {
                using (var db = new MenuStolovayaDBEntities())
                {
                    var query = from тр in db.Требования_накладные
                                join д in db.Документы on тр.Документ_id equals д.id
                                join м in db.Меню_на_день on тр.Меню_id equals м.id
                                join п_техн in db.Пользователи on тр.Технолог_id equals п_техн.id
                                from п_клад in db.Пользователи.Where(x => x.id == тр.Кладовщик_id).DefaultIfEmpty()
                                select new RequestDisplay
                                {
                                    Id = тр.id,
                                    Номер = тр.Номер,
                                    Дата_документа = д.Дата_документа,
                                    Дата_меню = м.Дата,
                                    Технолог = п_техн.Фамилия + " " + п_техн.Имя,
                                    Статус_требования = тр.Статус_требования,
                                    Кладовщик = п_клад != null ? п_клад.Фамилия + " " + п_клад.Имя : "",
                                    Дата_обработки = тр.Дата_обработки ?? DateTime.MinValue,
                                    Общее_количество = (decimal)(db.Строки_документов
                                        .Where(sd => sd.Документ_id == д.id)
                                        .Sum(sd => (decimal?)sd.Количество) ?? 0),
                                    Общая_сумма = (decimal)(db.Строки_документов
                                        .Where(sd => sd.Документ_id == д.id)
                                        .Sum(sd => (decimal?)sd.Сумма) ?? 0),
                                    Количество_позиций = db.Строки_документов
                                        .Count(sd => sd.Документ_id == д.id)
                                };

                    _requests = query.ToList();
                    ApplyRequestFilter();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки требований: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ApplyRequestFilter()
        {
            if (_requests == null) return;

            var filtered = _requests.AsEnumerable();

            string search = RequestSearchBox.Text?.ToLower() ?? "";
            if (!string.IsNullOrWhiteSpace(search))
            {
                filtered = filtered.Where(r =>
                    r.Номер.ToLower().Contains(search) ||
                    r.Технолог.ToLower().Contains(search) ||
                    r.Статус_требования.ToLower().Contains(search));
            }

            var selectedStatus = (RequestStatusFilter.SelectedItem as ComboBoxItem)?.Content?.ToString();
            if (selectedStatus != null && selectedStatus != "Все статусы")
            {
                filtered = filtered.Where(r => r.Статус_требования == selectedStatus);
            }

            RequestsDataGrid.ItemsSource = filtered.ToList();

            TotalRequestsText.Text = _requests.Count.ToString();
            PendingRequestsText.Text = _requests.Count(r => r.Статус_требования == "Ожидает").ToString();
            ApprovedRequestsText.Text = _requests.Count(r => r.Статус_требования == "Подтверждено").ToString();
            RejectedRequestsText.Text = _requests.Count(r => r.Статус_требования == "Отклонено").ToString();
        }

        private void RequestsDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            bool hasSelection = RequestsDataGrid.SelectedItem != null;
            ViewRequestDetailsButton.IsEnabled = hasSelection;

            if (hasSelection)
            {
                var selected = RequestsDataGrid.SelectedItem as RequestDisplay;
                bool isPending = selected?.Статус_требования == "Ожидает";
                ApproveRequestButton.IsEnabled = isPending;
                RejectRequestButton.IsEnabled = isPending;
            }
            else
            {
                ApproveRequestButton.IsEnabled = false;
                RejectRequestButton.IsEnabled = false;
            }
        }

        private void ViewRequestDetailsButton_Click(object sender, RoutedEventArgs e)
        {
            var selected = RequestsDataGrid.SelectedItem as RequestDisplay;
            if (selected != null)
            {
                var dialog = new RequestDetailsDialog(selected.Id);
                dialog.ShowDialog();
                LoadRequests();
            }
        }

        private void ApproveRequestButton_Click(object sender, RoutedEventArgs e)
        {
            var selected = RequestsDataGrid.SelectedItem as RequestDisplay;
            if (selected != null)
            {
                var result = MessageBox.Show(
                    $"Подтвердить требование №{selected.Номер}?\n\n" +
                    $"После подтверждения продукты будут списаны со склада.",
                    "Подтверждение",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        using (var db = new MenuStolovayaDBEntities())
                        {
                            var request = db.Требования_накладные.Find(selected.Id);
                            if (request != null)
                            {
                                var document = db.Документы.Find(request.Документ_id);
                                if (document != null && document.Статус != "Проведен")
                                {
                                    document.Статус = "Проведен";
                                    document.Кто_провел_id = ThisUser.CurrentUser.Id;
                                    document.Дата_проведения = DateTime.Now;

                                    request.Статус_требования = "Подтверждено";
                                    request.Кладовщик_id = ThisUser.CurrentUser.Id;
                                    request.Дата_обработки = DateTime.Now;

                                    db.SaveChanges();

                                    MessageBox.Show($"Требование №{selected.Номер} подтверждено",
                                        "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                                    LoadRequests();
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка при подтверждении: {ex.Message}", "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private void RejectRequestButton_Click(object sender, RoutedEventArgs e)
        {
            var selected = RequestsDataGrid.SelectedItem as RequestDisplay;
            if (selected != null)
            {
                var inputDialog = new InputDialog("Причина отклонения:", "Укажите причину отклонения требования");
                if (inputDialog.ShowDialog() == true && !string.IsNullOrWhiteSpace(inputDialog.Answer))
                {
                    try
                    {
                        using (var db = new MenuStolovayaDBEntities())
                        {
                            var request = db.Требования_накладные.Find(selected.Id);
                            if (request != null)
                            {
                                var document = db.Документы.Find(request.Документ_id);
                                if (document != null && document.Статус != "Отменен")
                                {
                                    document.Статус = "Отменен";
                                    document.Комментарий = inputDialog.Answer;

                                    request.Статус_требования = "Отклонено";
                                    request.Кладовщик_id = ThisUser.CurrentUser.Id;
                                    request.Дата_обработки = DateTime.Now;
                                    request.Комментарий_кладовщика = inputDialog.Answer;

                                    db.SaveChanges();

                                    MessageBox.Show($"Требование №{selected.Номер} отклонено",
                                        "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                                    LoadRequests();
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка при отклонении: {ex.Message}", "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private void RequestSearchBox_TextChanged(object sender, TextChangedEventArgs e) => ApplyRequestFilter();
        private void RequestStatusFilter_SelectionChanged(object sender, SelectionChangedEventArgs e) => ApplyRequestFilter();
        private void RefreshRequestsButton_Click(object sender, RoutedEventArgs e) => LoadRequests();

        #endregion

        #region Текущие остатки

        private void LoadStocks()
        {
            try
            {
                using (var db = new MenuStolovayaDBEntities())
                {
                    var stocksData = db.vw_Текущие_остатки.ToList();
                    _stocks = stocksData.Select(s => new StockDisplay
                    {
                        Склад_id = s.Склад_id,
                        Склад = s.Склад,
                        Артикул = s.Артикул,
                        Продукт = s.Продукт,
                        Категория = s.Категория,
                        Единица_измерения = s.Единица_измерения,
                        Количество = s.Количество,
                        Цена_за_единицу = s.Цена_за_единицу,
                        Сумма = s.Сумма,
                        Цена_последняя_закупка = s.Цена_последняя_закупка ?? 0,
                        Дата_обновления = s.Дата_обновления ?? DateTime.Now
                    }).ToList();

                    ApplyStockFilter();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки остатков: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ApplyStockFilter()
        {
            if (_stocks == null) return;

            var filtered = _stocks.AsEnumerable();

            string search = StockSearchBox.Text?.ToLower() ?? "";
            if (!string.IsNullOrWhiteSpace(search))
            {
                filtered = filtered.Where(s =>
                    s.Продукт.ToLower().Contains(search) ||
                    (s.Артикул?.ToLower().Contains(search) ?? false) ||
                    s.Склад.ToLower().Contains(search));
            }

            if (WarehouseFilter.SelectedValue != null && (int)WarehouseFilter.SelectedValue > 0)
            {
                int warehouseId = (int)WarehouseFilter.SelectedValue;
                filtered = filtered.Where(s => s.Склад_id == warehouseId);
            }

            StocksDataGrid.ItemsSource = filtered.ToList();

            TotalStockValueText.Text = $"{filtered.Sum(s => s.Сумма):N2} руб.";
            UniqueProductsText.Text = filtered.Select(s => s.Продукт).Distinct().Count().ToString();
        }

        private void StockSearchBox_TextChanged(object sender, TextChangedEventArgs e) => ApplyStockFilter();
        private void WarehouseFilter_SelectionChanged(object sender, SelectionChangedEventArgs e) => ApplyStockFilter();
        private void RefreshStocksButton_Click(object sender, RoutedEventArgs e) => LoadStocks();

        #endregion

        #region Управление продуктами

        private void LoadStorekeeperProducts()
        {
            try
            {
                // Ищем TextBox по имени - он есть в XAML с именем "ProductSearchBox"
                var searchBox = this.FindName("ProductSearchBox") as TextBox;
                string searchText = searchBox?.Text ?? "";

                _storekeeperProducts = _productService.GetProducts(searchText);
                StorekeeperProductsDataGrid.ItemsSource = _storekeeperProducts;
                UpdateProductStats();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке продуктов: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateProductStats()
        {
            if (_storekeeperProducts == null) return;

            TotalProductsText.Text = _storekeeperProducts.Count.ToString();
            ApprovedPriceProductsText.Text = _storekeeperProducts.Count(p => p.Утверждена_цена).ToString();
        }

        private void StorekeeperProductSearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            LoadStorekeeperProducts();
        }

        private void StorekeeperProductsDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            bool hasSelection = StorekeeperProductsDataGrid.SelectedItem != null;
            StorekeeperEditProductButton.IsEnabled = hasSelection;
            StorekeeperDeleteProductButton.IsEnabled = hasSelection;
        }

        private void StorekeeperAddProductButton_Click(object sender, RoutedEventArgs e)
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
                        LoadStorekeeperProducts();
                        LoadStocks();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при добавлении продукта: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void StorekeeperEditProductButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedProduct = StorekeeperProductsDataGrid.SelectedItem as ProductDisplay;
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
                                    LoadStorekeeperProducts();
                                    LoadStocks();
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
                    MessageBox.Show($"Ошибка при редактировании продукта: {ex.Message}", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                MessageBox.Show("Выберите продукт для редактирования", "Внимание",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void StorekeeperDeleteProductButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedProduct = StorekeeperProductsDataGrid.SelectedItem as ProductDisplay;
            if (selectedProduct != null)
            {
                using (var db = new MenuStolovayaDBEntities())
                {
                    var hasStock = db.Остатки_продуктов
                        .Any(o => o.Продукт_id == selectedProduct.Id && o.Количество > 0);

                    if (hasStock)
                    {
                        MessageBox.Show($"Нельзя удалить продукт \"{selectedProduct.Наименование}\", так как он имеет остатки на складе.\n" +
                                       "Сначала спишите остатки или переместите их.",
                            "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                }

                var result = MessageBox.Show($"Удалить продукт \"{selectedProduct.Наименование}\"?\n\n" +
                    "Продукт будет помечен как неактивный и скрыт из списка.",
                    "Подтверждение удаления", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    if (_productService.DeleteProduct(selectedProduct.Id))
                    {
                        MessageBox.Show("Продукт успешно удален", "Успех",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                        LoadStorekeeperProducts();
                    }
                }
            }
            else
            {
                MessageBox.Show("Выберите продукт для удаления", "Внимание",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void StorekeeperProductsDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var selectedProduct = StorekeeperProductsDataGrid.SelectedItem as ProductDisplay;
            if (selectedProduct != null)
            {
                try
                {
                    using (var db = new MenuStolovayaDBEntities())
                    {
                        var product = db.Продукты.Find(selectedProduct.Id);
                        if (product != null)
                        {
                            var editWindow = new AddEditProductWindow(product);
                            editWindow.ShowDialog();
                            LoadStorekeeperProducts();
                            LoadStocks();
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при просмотре продукта: {ex.Message}", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void StorekeeperRefreshProductsButton_Click(object sender, RoutedEventArgs e)
        {
            LoadStorekeeperProducts();
        }

        #endregion

        #region Приходы и Расходы

        private void LoadDocuments()
        {
            LoadIncomes();
            LoadExpenses();
        }

        private void LoadIncomes()
        {
            try
            {
                using (var db = new MenuStolovayaDBEntities())
                {
                    var query = from д in db.Документы
                                join с in db.Склады on д.Склад_получатель_id equals с.id into склады
                                from с in склады.DefaultIfEmpty()
                                join п in db.Пользователи on д.Кто_создал_id equals п.id
                                where д.Тип_документа == "Приход"
                                select new DocumentDisplay
                                {
                                    Id = д.id,
                                    Номер = д.Номер,
                                    Дата_документа = д.Дата_документа,
                                    Дата_создания = д.Дата_создания ?? DateTime.Now,
                                    Склад_отправитель = "",
                                    Склад_получатель = с != null ? с.Наименование : "",
                                    Статус = д.Статус,
                                    Кто_создал = п.Фамилия + " " + п.Имя,
                                    Сумма = (decimal)(db.Строки_документов
                                        .Where(sd => sd.Документ_id == д.id)
                                        .Sum(sd => (decimal?)sd.Сумма) ?? 0)
                                };

                    _incomes = query.OrderByDescending(d => d.Дата_создания).ToList();
                    IncomesDataGrid.ItemsSource = _incomes;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки приходов: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadExpenses()
        {
            try
            {
                using (var db = new MenuStolovayaDBEntities())
                {
                    var query = from д in db.Документы
                                join с in db.Склады on д.Склад_отправитель_id equals с.id into склады
                                from с in склады.DefaultIfEmpty()
                                join п in db.Пользователи on д.Кто_создал_id equals п.id
                                where д.Тип_документа == "Расход"
                                select new DocumentDisplay
                                {
                                    Id = д.id,
                                    Номер = д.Номер,
                                    Дата_документа = д.Дата_документа,
                                    Дата_создания = д.Дата_создания ?? DateTime.Now,
                                    Склад_отправитель = с != null ? с.Наименование : "",
                                    Склад_получатель = "",
                                    Статус = д.Статус,
                                    Кто_создал = п.Фамилия + " " + п.Имя,
                                    Сумма = (decimal)(db.Строки_документов
                                        .Where(sd => sd.Документ_id == д.id)
                                        .Sum(sd => (decimal?)sd.Сумма) ?? 0)
                                };

                    _expenses = query.OrderByDescending(d => d.Дата_создания).ToList();
                    ExpensesDataGrid.ItemsSource = _expenses;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки расходов: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void NewIncomeButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new WarehouseDocumentDialog("Приход");
            if (dialog.ShowDialog() == true)
            {
                LoadIncomes();
                LoadStocks();
            }
        }

        private void NewExpenseButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new WarehouseDocumentDialog("Расход");
            if (dialog.ShowDialog() == true)
            {
                LoadExpenses();
                LoadStocks();
            }
        }

        private void IncomesDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            bool hasSelection = IncomesDataGrid.SelectedItem != null;
            EditIncomeButton.IsEnabled = hasSelection;

            if (hasSelection)
            {
                var selected = IncomesDataGrid.SelectedItem as DocumentDisplay;
                PostIncomeButton.IsEnabled = selected?.Статус == "Черновик";
            }
            else
            {
                PostIncomeButton.IsEnabled = false;
            }
        }

        private void ExpensesDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            bool hasSelection = ExpensesDataGrid.SelectedItem != null;
            EditExpenseButton.IsEnabled = hasSelection;

            if (hasSelection)
            {
                var selected = ExpensesDataGrid.SelectedItem as DocumentDisplay;
                PostExpenseButton.IsEnabled = selected?.Статус == "Черновик";
            }
            else
            {
                PostExpenseButton.IsEnabled = false;
            }
        }

        private void EditIncomeButton_Click(object sender, RoutedEventArgs e)
        {
            var selected = IncomesDataGrid.SelectedItem as DocumentDisplay;
            if (selected != null && selected.Статус == "Черновик")
            {
                var dialog = new WarehouseDocumentDialog("Приход", selected.Id);
                if (dialog.ShowDialog() == true)
                {
                    LoadIncomes();
                    LoadStocks();
                }
            }
            else
            {
                MessageBox.Show("Редактировать можно только документы в статусе «Черновик»",
                    "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void EditExpenseButton_Click(object sender, RoutedEventArgs e)
        {
            var selected = ExpensesDataGrid.SelectedItem as DocumentDisplay;
            if (selected != null && selected.Статус == "Черновик")
            {
                var dialog = new WarehouseDocumentDialog("Расход", selected.Id);
                if (dialog.ShowDialog() == true)
                {
                    LoadExpenses();
                    LoadStocks();
                }
            }
            else
            {
                MessageBox.Show("Редактировать можно только документы в статусе «Черновик»",
                    "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void PostIncomeButton_Click(object sender, RoutedEventArgs e)
        {
            var selected = IncomesDataGrid.SelectedItem as DocumentDisplay;
            if (selected != null)
            {
                PostDocument(selected.Id);
            }
        }

        private void PostExpenseButton_Click(object sender, RoutedEventArgs e)
        {
            var selected = ExpensesDataGrid.SelectedItem as DocumentDisplay;
            if (selected != null)
            {
                PostDocument(selected.Id);
            }
        }

        private void PostDocument(int documentId)
        {
            var result = MessageBox.Show(
                "Провести документ?\n\nПосле проведения изменения будут невозможны.",
                "Подтверждение",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    using (var db = new MenuStolovayaDBEntities())
                    {
                        var document = db.Документы.Find(documentId);
                        if (document != null && document.Статус == "Черновик")
                        {
                            document.Статус = "Проведен";
                            document.Кто_провел_id = ThisUser.CurrentUser.Id;
                            document.Дата_проведения = DateTime.Now;
                            db.SaveChanges();

                            MessageBox.Show("Документ успешно проведен",
                                "Успех", MessageBoxButton.OK, MessageBoxImage.Information);

                            LoadIncomes();
                            LoadExpenses();
                            LoadStocks();
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при проведении: {ex.Message}", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void RefreshIncomesButton_Click(object sender, RoutedEventArgs e) => LoadIncomes();
        private void RefreshExpensesButton_Click(object sender, RoutedEventArgs e) => LoadExpenses();

        #endregion

        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            ThisUser.ClearCurrentUser();
            var loginWindow = new LoginWindow();
            loginWindow.Show();
            Close();
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
    }

    #region Display Classes

    public class RequestDisplay
    {
        public int Id { get; set; }
        public string Номер { get; set; }
        public DateTime Дата_документа { get; set; }
        public DateTime Дата_меню { get; set; }
        public string Технолог { get; set; }
        public string Статус_требования { get; set; }
        public string Кладовщик { get; set; }
        public DateTime? Дата_обработки { get; set; }
        public decimal Общее_количество { get; set; }
        public decimal Общая_сумма { get; set; }
        public int Количество_позиций { get; set; }
    }

    public class StockDisplay
    {
        public int Склад_id { get; set; }
        public string Склад { get; set; }
        public string Артикул { get; set; }
        public string Продукт { get; set; }
        public string Категория { get; set; }
        public string Единица_измерения { get; set; }
        public decimal Количество { get; set; }
        public decimal Цена_за_единицу { get; set; }
        public decimal Сумма { get; set; }
        public decimal Цена_последняя_закупка { get; set; }
        public DateTime Дата_обновления { get; set; }
    }

    public class DocumentDisplay
    {
        public int Id { get; set; }
        public string Номер { get; set; }
        public DateTime Дата_документа { get; set; }
        public DateTime Дата_создания { get; set; }
        public string Склад_отправитель { get; set; }
        public string Склад_получатель { get; set; }
        public string Статус { get; set; }
        public string Кто_создал { get; set; }
        public decimal Сумма { get; set; }
    }

    #endregion


}