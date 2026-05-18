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
        private Dictionary<Button, Style> _buttonStyles = new Dictionary<Button, Style>();
        private bool _isMaximized = false;
        private CalcCardService _calcCardService;
        private ProductPriceService _productPriceService;
        private AccountantTechCardService _techCardService;

        public AccountantWindow()
        {
            InitializeComponent();

            _calcCardService = new CalcCardService();
            _productPriceService = new ProductPriceService();
            _techCardService = new AccountantTechCardService();

            // Сохраняем стили кнопок
            _buttonStyles[CalcCardsTabButton] = CalcCardsTabButton.Style;
            _buttonStyles[PricesTabButton] = PricesTabButton.Style;
            _buttonStyles[TechCardsTabButton] = TechCardsTabButton.Style;
            _buttonStyles[HelpTabButton] = HelpTabButton.Style;

            LoadCurrentUserInfo();
            LoadData();

            this.Loaded += (s, e) => ShowTab(CalcCardsContent, CalcCardsTabButton);
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

        private void ShowTab(Grid tabContent, Button activeButton)
        {
            CalcCardsContent.Visibility = Visibility.Collapsed;
            PricesContent.Visibility = Visibility.Collapsed;
            TechCardsContent.Visibility = Visibility.Collapsed;
            HelpContent.Visibility = Visibility.Collapsed;

            tabContent.Visibility = Visibility.Visible;

            foreach (var btn in _buttonStyles.Keys)
            {
                btn.Style = _buttonStyles[btn];
            }

            activeButton.Style = (Style)FindResource("ActiveTabButtonStyle");
        }

        private void LoadData()
        {
            LoadCalcCards();
            LoadProducts();
            LoadTechCards();
        }

        #region Навигация

        private void CalcCardsTabButton_Click(object sender, RoutedEventArgs e)
        {
            ShowTab(CalcCardsContent, CalcCardsTabButton);
            LoadCalcCards();
        }

        private void PricesTabButton_Click(object sender, RoutedEventArgs e)
        {
            ShowTab(PricesContent, PricesTabButton);
            LoadProducts();
        }

        private void TechCardsTabButton_Click(object sender, RoutedEventArgs e)
        {
            ShowTab(TechCardsContent, TechCardsTabButton);
            LoadTechCards();
        }

        private void HelpTabButton_Click(object sender, RoutedEventArgs e)
        {
            ShowTab(HelpContent, HelpTabButton);
        }

        #endregion

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
                MessageBox.Show($"Ошибка при загрузке: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ReturnToReviewButton_Click(object sender, RoutedEventArgs e)
        {
            if (CalcCardsDataGrid.SelectedItem is CalcCardDisplay selectedCard)
            {
                var result = MessageBox.Show(
                    $"Отправить калькуляционную карточку {selectedCard.Номер} на пересмотр?\n\n" +
                    $"После этого её можно будет редактировать.",
                    "Подтверждение",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        if (_calcCardService.ReturnToReview(selectedCard.Id))
                        {
                            LoadCalcCards();
                            MessageBox.Show("Карточка отправлена на пересмотр", "Успех",
                                MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private void ReturnToDraftButton_Click(object sender, RoutedEventArgs e)
        {
            if (CalcCardsDataGrid.SelectedItem is CalcCardDisplay selectedCard)
            {
                var result = MessageBox.Show(
                    $"Вернуть калькуляционную карточку {selectedCard.Номер} в черновик?\n\n" +
                    $"После этого её можно будет утвердить заново.",
                    "Подтверждение",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        if (_calcCardService.ReturnToDraft(selectedCard.Id))
                        {
                            LoadCalcCards();
                            MessageBox.Show("Карточка возвращена в черновик", "Успех",
                                MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private void UpdateCalcCardStats()
        {
            if (_calcCards == null) return;

            TotalCalcCardsText.Text = _calcCards.Count.ToString();
            ApprovedCalcCardsText.Text = _calcCards.Count(c => c.Статус == "Утверждена").ToString();
            ReviewCalcCardsText.Text = _calcCards.Count(c => c.Статус == "На пересмотре").ToString();
            DraftCalcCardsText.Text = _calcCards.Count(c => c.Статус == "Черновик").ToString();

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
            DeleteCalcCardButton.IsEnabled = hasSelection;
            ViewCalcLinesButton.IsEnabled = hasSelection;
            ReturnToReviewButton.IsEnabled = hasSelection;
            ReturnToDraftButton.IsEnabled = hasSelection;

            if (hasSelection && CalcCardsDataGrid.SelectedItem is CalcCardDisplay selectedCard)
            {
                // Кнопка "Утвердить" - только для черновиков
                ApproveCalcCardButton.IsEnabled = selectedCard.Статус == "Черновик";

                // Кнопка "На пересмотр" - только для утверждённых
                ReturnToReviewButton.IsEnabled = selectedCard.Статус == "Утверждена";

                // Кнопка "В черновик" - только для карточек на пересмотре
                ReturnToDraftButton.IsEnabled = selectedCard.Статус == "На пересмотре";

                // Кнопка "Редактировать" - для черновиков и на пересмотре
                EditCalcCardButton.IsEnabled = selectedCard.Статус == "Черновик" || selectedCard.Статус == "На пересмотре";

                // Кнопка "Удалить" - всё кроме утверждённых
                DeleteCalcCardButton.IsEnabled = selectedCard.Статус != "Утверждена";
            }
            else
            {
                ApproveCalcCardButton.IsEnabled = false;
                ReturnToReviewButton.IsEnabled = false;
                ReturnToDraftButton.IsEnabled = false;
                EditCalcCardButton.IsEnabled = false;
                DeleteCalcCardButton.IsEnabled = false;
                ViewCalcLinesButton.IsEnabled = false;
            }
        }

        private void AddCalcCardButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dialog = new CreateCalcCardDialog();
                if (dialog.ShowDialog() == true) LoadCalcCards();
            }
            catch (Exception ex) { ShowError(ex); }
        }

        private void EditCalcCardButton_Click(object sender, RoutedEventArgs e)
        {
            if (CalcCardsDataGrid.SelectedItem is CalcCardDisplay card)
            {
                try
                {
                    var dialog = new EditCalcCardDialog(card.Id);
                    if (dialog.ShowDialog() == true) LoadCalcCards();
                }
                catch (Exception ex) { ShowError(ex); }
            }
        }

        private void ApproveCalcCardButton_Click(object sender, RoutedEventArgs e)
        {
            if (CalcCardsDataGrid.SelectedItem is CalcCardDisplay card && Confirm($"утвердить карточку {card.Номер}"))
            {
                if (_calcCardService.ApproveCalcCard(card.Id)) LoadCalcCards();
            }
        }

        private void DeleteCalcCardButton_Click(object sender, RoutedEventArgs e)
        {
            if (CalcCardsDataGrid.SelectedItem is CalcCardDisplay card && Confirm($"удалить карточку {card.Номер}", true))
            {
                if (_calcCardService.DeleteCalcCard(card.Id)) LoadCalcCards();
            }
        }

        private void ViewCalcLinesButton_Click(object sender, RoutedEventArgs e)
        {
            if (CalcCardsDataGrid.SelectedItem is CalcCardDisplay card)
            {
                var dialog = new CalcLinesDialog(card.Id);
                dialog.ShowDialog();
            }
        }

        private void RefreshCalcCardsButton_Click(object sender, RoutedEventArgs e) => LoadCalcCards();
        private void CalcCardSearchBox_TextChanged(object sender, TextChangedEventArgs e) => FilterCalcCards();

        private void FilterCalcCards()
        {
            if (_calcCards == null) return;
            string search = CalcCardSearchBox.Text?.ToLower() ?? "";
            CalcCardsDataGrid.ItemsSource = string.IsNullOrWhiteSpace(search) ? _calcCards :
                _calcCards.Where(c => c.Номер.ToLower().Contains(search) || c.Блюдо.ToLower().Contains(search)).ToList();
        }

        #endregion

        #region Управление ценами

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
            catch (Exception ex) { ShowError(ex); }
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

            if (hasSelection && ProductsDataGrid.SelectedItem is AccountantProductPriceDisplay p)
                ApprovePriceButton.IsEnabled = !p.Утверждена_цена;
        }

        private void ApprovePriceButton_Click(object sender, RoutedEventArgs e)
        {
            if (ProductsDataGrid.SelectedItem is AccountantProductPriceDisplay p && Confirm($"утвердить цену продукта \"{p.Наименование}\" ({p.Цена:N2} руб.)"))
            {
                if (_productPriceService.ApproveProductPrice(p.Id, p.Цена)) LoadProducts();
            }
        }

        private void EditPriceButton_Click(object sender, RoutedEventArgs e)
        {
            if (ProductsDataGrid.SelectedItem is AccountantProductPriceDisplay p)
            {
                var dialog = new EditProductPriceDialog(p.Id, p.Цена);
                if (dialog.ShowDialog() == true) LoadProducts();
            }
        }

        private void RefreshProductsButton_Click(object sender, RoutedEventArgs e) => LoadProducts();
        private void ProductPriceSearchBox_TextChanged(object sender, TextChangedEventArgs e) => LoadProducts();
        private void PriceStatusFilterCombo_SelectionChanged(object sender, SelectionChangedEventArgs e) => FilterProducts();

        private void FilterProducts()
        {
            if (_products == null) return;
            int index = PriceStatusFilterCombo.SelectedIndex;
            ProductsDataGrid.ItemsSource = index == 1 ? _products.Where(p => p.Утверждена_цена).ToList() :
                                          index == 2 ? _products.Where(p => !p.Утверждена_цена).ToList() : _products;
        }

        #endregion

        #region Технологические карты

        private void LoadTechCards()
        {
            try
            {
                _techCards = _techCardService.GetTechCards(TechCardSearchBox.Text);
                TechCardsDataGrid.ItemsSource = _techCards;
                FilterTechCards();
            }
            catch (Exception ex) { ShowError(ex); }
        }

        private void TechCardsDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            bool hasSelection = TechCardsDataGrid.SelectedItem != null;
            ApproveTechCardButton.IsEnabled = hasSelection;
            ViewTechCardDetailsButton.IsEnabled = hasSelection;

            if (hasSelection && TechCardsDataGrid.SelectedItem is AccountantTechCardDisplay card)
                ApproveTechCardButton.IsEnabled = card.Статус != "Утверждена";
        }

        private void ApproveTechCardButton_Click(object sender, RoutedEventArgs e)
        {
            if (TechCardsDataGrid.SelectedItem is AccountantTechCardDisplay card && Confirm($"утвердить тех. карту {card.Номер}"))
            {
                if (_techCardService.ApproveTechCard(card.Id) && _calcCardService.CreateCalcCardFromTechCard(card.Id))
                {
                    LoadTechCards();
                    LoadCalcCards();
                    MessageBox.Show("Тех. карта утверждена и создана калькуляционная карточка", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }

        private void ViewTechCardDetailsButton_Click(object sender, RoutedEventArgs e)
        {
            if (TechCardsDataGrid.SelectedItem is AccountantTechCardDisplay card)
            {
                var dialog = new TechCardDetailsDialog(card.Id);
                dialog.ShowDialog();
            }
        }

        private void RefreshTechCardsButton_Click(object sender, RoutedEventArgs e) => LoadTechCards();
        private void TechCardSearchBox_TextChanged(object sender, TextChangedEventArgs e) => FilterTechCards();
        private void TechCardStatusCombo_SelectionChanged(object sender, SelectionChangedEventArgs e) => FilterTechCards();

        private void FilterTechCards()
        {
            if (_techCards == null) return;
            string search = TechCardSearchBox.Text?.ToLower() ?? "";
            string status = (TechCardStatusCombo.SelectedItem as ComboBoxItem)?.Content?.ToString();

            var filtered = _techCards.AsEnumerable();
            if (!string.IsNullOrWhiteSpace(search))
                filtered = filtered.Where(c => c.Номер.ToLower().Contains(search) || c.Блюдо.ToLower().Contains(search));
            if (status != null && status != "Все статусы")
                filtered = filtered.Where(c => c.Статус == status);

            TechCardsDataGrid.ItemsSource = filtered.ToList();
        }

        #endregion

        #region Вспомогательные методы

        private bool Confirm(string action, bool isDanger = false)
        {
            return MessageBox.Show($"Вы уверены, что хотите {action}?", "Подтверждение",
                MessageBoxButton.YesNo, isDanger ? MessageBoxImage.Warning : MessageBoxImage.Question) == MessageBoxResult.Yes;
        }

        private void ShowError(Exception ex) => MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        private void HelpButton_Click(object sender, RoutedEventArgs e) => OpenHelp();
        private void OpenHelp() => System.Diagnostics.Process.Start(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Help", "help.html"));

        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            ThisUser.ClearCurrentUser();
            new LoginWindow().Show();
            Close();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            if (Confirm("выйти из программы", false)) Application.Current.Shutdown();
        }

        #endregion
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
        public Brush ЦветСтатуса { get; set; }
    }
}