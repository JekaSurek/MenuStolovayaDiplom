using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using MenuStolovaya.Models;
using MenuStolovaya.Services;

namespace MenuStolovaya.Views
{
    public partial class AccountantWindow : Window
    {
        private List<CalcCardDisplay> _calcCards;
        private List<AccountantProductPriceDisplay> _products;
        private List<AccountantTechCardDisplay> _techCards;

        private CalcCardService _calcCardService;
        private ProductPriceService _productPriceService;
        private AccountantTechCardService _techCardService;

        public AccountantWindow()
        {
            InitializeComponent();
            LoadCurrentUserInfo();
            InitializeServices();
            LoadData();
        }

        private void LoadCurrentUserInfo()
        {
            CurrentUserText.Text = ThisUser.CurrentUser?.FullName ?? "Неизвестно";
        }

        private void InitializeServices()
        {
            _calcCardService = new CalcCardService();
            _productPriceService = new ProductPriceService();
            _techCardService = new AccountantTechCardService();
        }

        private void LoadData()
        {
            // Загружаем данные для всех вкладок
            LoadCalcCards();
            LoadProducts();
            LoadTechCards();
        }

        #region Калькуляционные карточки

        private void LoadCalcCards()
        {
            try
            {
                _calcCards = _calcCardService.GetCalcCards();
                CalcCardsDataGrid.ItemsSource = _calcCards;
                UpdateCalcCardStats();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке калькуляционных карточек: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateCalcCardStats()
        {
            if (_calcCards == null) return;

            TotalCalcCardsText.Text = _calcCards.Count.ToString();
            ApprovedCalcCardsText.Text = _calcCards.Count(c => c.Статус == "Утверждена").ToString();

            var approvedCards = _calcCards.Where(c => c.Статус == "Утверждена" && c.Фудкост_процент.HasValue).ToList();
            if (approvedCards.Any())
            {
                AvgFoodCostText.Text = $"{approvedCards.Average(c => c.Фудкост_процент.Value):N2}%";
            }
            else
            {
                AvgFoodCostText.Text = "Нет данных";
            }
        }

        private void CalcCardsDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            bool hasSelection = CalcCardsDataGrid.SelectedItem != null;
            EditCalcCardButton.IsEnabled = hasSelection;
            ApproveCalcCardButton.IsEnabled = hasSelection;
            ReturnToReviewButton.IsEnabled = hasSelection;
            ReturnToDraftButton.IsEnabled = hasSelection;
            DeleteCalcCardButton.IsEnabled = hasSelection;
            ViewCalcLinesButton.IsEnabled = hasSelection;

            if (hasSelection)
            {
                var selectedCard = (CalcCardDisplay)CalcCardsDataGrid.SelectedItem;
                ApproveCalcCardButton.IsEnabled = selectedCard.Статус == "Черновик";
                ReturnToReviewButton.IsEnabled = selectedCard.Статус == "Утверждена";
                ReturnToDraftButton.IsEnabled = selectedCard.Статус == "На пересмотре";
                DeleteCalcCardButton.IsEnabled = selectedCard.Статус != "Утверждена";
                EditCalcCardButton.IsEnabled = selectedCard.Статус != "Утверждена";
            }
        }

        private void AddCalcCardButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dialog = new CreateCalcCardDialog();
                if (dialog.ShowDialog() == true)
                {
                    LoadCalcCards();
                    MessageBox.Show("Калькуляционная карточка успешно создана",
                        "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при создании калькуляционной карточки: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void EditCalcCardButton_Click(object sender, RoutedEventArgs e)
        {
            if (CalcCardsDataGrid.SelectedItem is CalcCardDisplay selectedCard)
            {
                try
                {
                    var dialog = new EditCalcCardDialog(selectedCard.Id);
                    if (dialog.ShowDialog() == true)
                    {
                        LoadCalcCards();
                        MessageBox.Show("Калькуляционная карточка успешно обновлена",
                            "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при редактировании калькуляционной карточки: {ex.Message}",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ApproveCalcCardButton_Click(object sender, RoutedEventArgs e)
        {
            if (CalcCardsDataGrid.SelectedItem is CalcCardDisplay selectedCard)
            {
                try
                {
                    var result = MessageBox.Show(
                        $"Вы уверены, что хотите утвердить калькуляционную карточку {selectedCard.Номер}?\n" +
                        $"После утверждения изменения будут невозможны.",
                        "Подтверждение утверждения",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);

                    if (result == MessageBoxResult.Yes)
                    {
                        if (_calcCardService.ApproveCalcCard(selectedCard.Id))
                        {
                            LoadCalcCards();
                            MessageBox.Show("Калькуляционная карточка успешно утверждена",
                                "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при утверждении калькуляционной карточки: {ex.Message}",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ViewCalcLinesButton_Click(object sender, RoutedEventArgs e)
        {
            if (CalcCardsDataGrid.SelectedItem is CalcCardDisplay selectedCard)
            {
                try
                {
                    var dialog = new CalcLinesDialog(selectedCard.Id);
                    dialog.ShowDialog();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при загрузке строк калькуляции: {ex.Message}",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void RefreshCalcCardsButton_Click(object sender, RoutedEventArgs e)
        {
            LoadCalcCards();
        }

        private void CalcCardSearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_calcCards != null)
            {
                string searchText = CalcCardSearchBox.Text.ToLower();
                if (string.IsNullOrWhiteSpace(searchText))
                {
                    CalcCardsDataGrid.ItemsSource = _calcCards;
                }
                else
                {
                    CalcCardsDataGrid.ItemsSource = _calcCards.Where(c =>
                        c.Номер.ToLower().Contains(searchText) ||
                        c.Блюдо.ToLower().Contains(searchText) ||
                        c.Статус.ToLower().Contains(searchText)).ToList();
                }
            }
        }

        #endregion

        #region Управление ценами продуктов

        private void LoadProducts()
        {
            try
            {
                var products = _productPriceService.GetProducts(ProductPriceSearchBox.Text);
                _products = products.Select(p => new AccountantProductPriceDisplay
                {
                    Id = p.Id,
                    Артикул = p.Артикул,
                    Наименование = p.Наименование,
                    Категория = p.Категория,
                    Единица_измерения = p.Единица_измерения,
                    Цена = p.Цена,
                    Утверждена_цена = p.Утверждена_цена,
                    Дата_утверждения_цены = p.Дата_утверждения_цены,
                    Кто_утвердил_цену = p.Кто_утвердил_цену,
                    СтатусЦены = p.Утверждена_цена ? "Утверждена" : "На утверждении",
                    ЦветСтатуса = p.Утверждена_цена ? Brushes.Green : Brushes.Orange
                }).ToList();

                ProductsDataGrid.ItemsSource = _products;
                UpdateProductPriceStats();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке продуктов: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateProductPriceStats()
        {
            if (_products == null) return;

            TotalProductsText.Text = _products.Count.ToString();
            ApprovedPricesText.Text = _products.Count(p => p.Утверждена_цена).ToString();
            PendingPricesText.Text = _products.Count(p => !p.Утверждена_цена).ToString();
        }

        private void ProductsDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            bool hasSelection = ProductsDataGrid.SelectedItem != null;
            ApprovePriceButton.IsEnabled = hasSelection;
            EditPriceButton.IsEnabled = hasSelection;

            if (hasSelection)
            {
                var selectedProduct = (AccountantProductPriceDisplay)ProductsDataGrid.SelectedItem;
                ApprovePriceButton.IsEnabled = !selectedProduct.Утверждена_цена;
            }
        }

        private void ApprovePriceButton_Click(object sender, RoutedEventArgs e)
        {
            if (ProductsDataGrid.SelectedItem is AccountantProductPriceDisplay selectedProduct)
            {
                try
                {
                    var result = MessageBox.Show(
                        $"Вы уверены, что хотите утвердить цену продукта \"{selectedProduct.Наименование}\"?\n" +
                        $"Текущая цена: {selectedProduct.Цена:N2} руб. {selectedProduct.Единица_измерения}\n" +
                        $"После утверждения цена будет использована при расчетах.",
                        "Подтверждение утверждения цены",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);

                    if (result == MessageBoxResult.Yes)
                    {
                        if (_productPriceService.ApproveProductPrice(selectedProduct.Id, selectedProduct.Цена))
                        {
                            LoadProducts();
                            MessageBox.Show("Цена продукта успешно утверждена",
                                "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при утверждении цены продукта: {ex.Message}",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void EditPriceButton_Click(object sender, RoutedEventArgs e)
        {
            if (ProductsDataGrid.SelectedItem is AccountantProductPriceDisplay selectedProduct)
            {
                try
                {
                    var dialog = new EditProductPriceDialog(selectedProduct.Id, selectedProduct.Цена);
                    if (dialog.ShowDialog() == true)
                    {
                        LoadProducts();
                        MessageBox.Show("Цена продукта успешно обновлена",
                            "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при изменении цены продукта: {ex.Message}",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void RefreshProductsButton_Click(object sender, RoutedEventArgs e)
        {
            LoadProducts();
        }

        private void ProductPriceSearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            LoadProducts();
        }

        private void PriceStatusFilterCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_products == null || ProductsDataGrid == null) return;

            var selectedIndex = PriceStatusFilterCombo.SelectedIndex;
            switch (selectedIndex)
            {
                case 1: // Только утвержденные
                    ProductsDataGrid.ItemsSource = _products.Where(p => p.Утверждена_цена).ToList();
                    break;
                case 2: // Только неутвержденные
                    ProductsDataGrid.ItemsSource = _products.Where(p => !p.Утверждена_цена).ToList();
                    break;
                default: // Все цены
                    ProductsDataGrid.ItemsSource = _products;
                    break;
            }
        }

        #endregion

        #region Технологические карты

        private void LoadTechCards()
        {
            try
            {
                _techCards = _techCardService.GetTechCards(TechCardSearchBox.Text);
                TechCardsDataGrid.ItemsSource = _techCards;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке технологических карт: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void TechCardsDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            bool hasSelection = TechCardsDataGrid.SelectedItem != null;
            ApproveTechCardButton.IsEnabled = hasSelection;
            ViewTechCardDetailsButton.IsEnabled = hasSelection;

            if (hasSelection)
            {
                var selectedCard = (AccountantTechCardDisplay)TechCardsDataGrid.SelectedItem;
                ApproveTechCardButton.IsEnabled = selectedCard.Статус != "Утверждена";
            }
        }

        private void ApproveTechCardButton_Click(object sender, RoutedEventArgs e)
        {
            if (TechCardsDataGrid.SelectedItem is AccountantTechCardDisplay selectedCard)
            {
                try
                {
                    var result = MessageBox.Show(
                        $"Вы уверены, что хотите утвердить технологическую карту {selectedCard.Номер}?\n" +
                        $"Блюдо: {selectedCard.Блюдо}\n" +
                        $"Выход: {selectedCard.Выход} г\n" +
                        $"После утверждения будет создана калькуляционная карточка.",
                        "Подтверждение утверждения",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);

                    if (result == MessageBoxResult.Yes)
                    {
                        if (_techCardService.ApproveTechCard(selectedCard.Id))
                        {
                            // Автоматически создаем калькуляционную карточку
                            if (_calcCardService.CreateCalcCardFromTechCard(selectedCard.Id))
                            {
                                LoadTechCards();
                                LoadCalcCards();
                                MessageBox.Show("Технологическая карта утверждена и создана калькуляционная карточка",
                                    "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при утверждении технологической карты: {ex.Message}",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ViewTechCardDetailsButton_Click(object sender, RoutedEventArgs e)
        {
            if (TechCardsDataGrid.SelectedItem is AccountantTechCardDisplay selectedCard)
            {
                try
                {
                    var dialog = new TechCardDetailsDialog(selectedCard.Id);
                    dialog.ShowDialog();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при загрузке деталей технологической карты: {ex.Message}",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void RefreshTechCardsButton_Click(object sender, RoutedEventArgs e)
        {
            LoadTechCards();
        }

        private void TechCardSearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            LoadTechCards();
        }

        private void TechCardStatusCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            LoadTechCards();
        }

        #endregion

        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            ThisUser.ClearCurrentUser();
            var loginWindow = new LoginWindow();
            loginWindow.Show();
            this.Close();
        }

        private void DeleteCalcCardButton_Click(object sender, RoutedEventArgs e)
        {
            if (CalcCardsDataGrid.SelectedItem is CalcCardDisplay selectedCard)
            {
                try
                {
                    var result = MessageBox.Show(
                        $"Вы уверены, что хотите удалить калькуляционную карточку {selectedCard.Номер}?\n" +
                        $"Блюдо: {selectedCard.Блюдо}\n" +
                        $"Эта операция необратима.",
                        "Подтверждение удаления",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Warning);

                    if (result == MessageBoxResult.Yes)
                    {
                        if (_calcCardService.DeleteCalcCard(selectedCard.Id))
                        {
                            LoadCalcCards();
                            MessageBox.Show("Калькуляционная карточка успешно удалена",
                                "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при удалении калькуляционной карточки: {ex.Message}",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ReturnToReviewButton_Click(object sender, RoutedEventArgs e)
        {
            if (CalcCardsDataGrid.SelectedItem is CalcCardDisplay selectedCard)
            {
                try
                {
                    var result = MessageBox.Show(
                        $"Вы уверены, что хотите отправить калькуляционную карточку {selectedCard.Номер} на пересмотр?\n" +
                        $"После этого она будет доступна для редактирования.",
                        "Подтверждение отправки на пересмотр",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);

                    if (result == MessageBoxResult.Yes)
                    {
                        if (_calcCardService.ReturnToReview(selectedCard.Id))
                        {
                            LoadCalcCards();
                            MessageBox.Show("Калькуляционная карточка отправлена на пересмотр",
                                "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при отправке на пересмотр: {ex.Message}",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ReturnToDraftButton_Click(object sender, RoutedEventArgs e)
        {
            if (CalcCardsDataGrid.SelectedItem is CalcCardDisplay selectedCard)
            {
                try
                {
                    var result = MessageBox.Show(
                        $"Вы уверены, что хотите вернуть калькуляционную карточку {selectedCard.Номер} в черновик?\n" +
                        $"После этого её можно будет утвердить заново.",
                        "Подтверждение возврата в черновик",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);

                    if (result == MessageBoxResult.Yes)
                    {
                        if (_calcCardService.ReturnToDraft(selectedCard.Id))
                        {
                            LoadCalcCards();
                            MessageBox.Show("Калькуляционная карточка возвращена в черновик",
                                "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при возврате в черновик: {ex.Message}",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
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
    }

    public class AccountantProductPriceDisplay
    {
        public int Id { get; set; }
        public string Артикул { get; set; }
        public string Наименование { get; set; }
        public string Категория { get; set; }
        public string Единица_измерения { get; set; }
        public decimal Цена { get; set; }
        public bool Утверждена_цена { get; set; }
        public DateTime? Дата_утверждения_цены { get; set; }
        public string Кто_утвердил_цену { get; set; }
        public string СтатусЦены { get; set; }
        public System.Windows.Media.Brush ЦветСтатуса { get; set; }
    }
}