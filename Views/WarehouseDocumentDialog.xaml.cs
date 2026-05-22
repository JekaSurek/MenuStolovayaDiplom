using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.ComponentModel;
using MenuStolovaya.Models;

namespace MenuStolovaya.Views
{
    public partial class WarehouseDocumentDialog : Window, INotifyPropertyChanged
    {
        private string _documentType;
        private int? _documentId;
        private DateTime _date = DateTime.Today;
        private List<DocumentLineItem> _lines = new List<DocumentLineItem>();

        public event PropertyChangedEventHandler PropertyChanged;

        public DateTime Date
        {
            get => _date;
            set { _date = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Date))); }
        }

        public WarehouseDocumentDialog(string documentType, int? documentId = null)
        {
            InitializeComponent();
            DataContext = this;
            _documentType = documentType;
            _documentId = documentId;

            TitleText.Text = documentType == "Приход" ? "Новый приход" : "Новый расход";
            if (documentId.HasValue)
                TitleText.Text = documentType == "Приход" ? "Редактирование прихода" : "Редактирование расхода";

            LoadData();
            if (documentId.HasValue)
                LoadDocument();
        }

        private void LoadData()
        {
            try
            {
                using (var db = new MenuStolovayaDBEntities())
                {
                    // Загрузка складов
                    var warehouses = db.Склады
                        .Where(s => s.Активен == true)
                        .Select(s => new { Id = s.id, Name = s.Наименование })
                        .ToList();
                    WarehouseCombo.ItemsSource = warehouses;
                    WarehouseCombo.DisplayMemberPath = "Name";
                    WarehouseCombo.SelectedValuePath = "Id";

                    // Загрузка продуктов для выбора - СНАЧАЛА ПОЛУЧАЕМ ДАННЫЕ, ПОТОМ ФОРМАТИРУЕМ
                    var productsRaw = db.Продукты
                        .Where(p => p.Активен == true)
                        .Select(p => new
                        {
                            p.id,
                            p.Артикул,
                            p.Наименование,
                            p.Единица_измерения,
                            Цена = p.Цена ?? 0m
                        })
                        .ToList(); // <-- ВАЖНО: сначала выполняем запрос

                    // Теперь форматируем DisplayText в памяти
                    var products = productsRaw.Select(p => new
                    {
                        p.id,
                        DisplayText = $"{p.Артикул} - {p.Наименование}",
                        p.Единица_измерения,
                        p.Цена
                    }).ToList();

                    ProductCombo.ItemsSource = products;
                    ProductCombo.DisplayMemberPath = "DisplayText";
                    ProductCombo.SelectedValuePath = "id";

                    // Генерация номера документа
                    if (!_documentId.HasValue)
                    {
                        string prefix = _documentType == "Приход" ? "ПРИХ" : "РАСХ";
                        string datePart = DateTime.Now.ToString("yyyyMMdd");
                        int count = db.Документы.Count(d => d.Тип_документа == _documentType &&
                                                           d.Дата_создания >= DateTime.Today) + 1;
                        NumberText.Text = $"{prefix}-{datePart}-{count:D4}";
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadDocument()
        {
            try
            {
                using (var db = new MenuStolovayaDBEntities())
                {
                    var document = db.Документы.Find(_documentId.Value);
                    if (document != null)
                    {
                        NumberText.Text = document.Номер;
                        Date = document.Дата_документа;
                        WarehouseCombo.SelectedValue = _documentType == "Приход"
                            ? document.Склад_получатель_id
                            : document.Склад_отправитель_id;

                        // Сначала получаем данные
                        var linesRaw = db.Строки_документов
                            .Where(sd => sd.Документ_id == _documentId.Value)
                            .Join(db.Продукты, sd => sd.Продукт_id, p => p.id, (sd, p) => new
                            {
                                p.id,
                                p.Артикул,
                                p.Наименование,
                                p.Единица_измерения,
                                sd.Количество,
                                sd.Цена,
                                sd.Сумма
                            })
                            .ToList();

                        // Затем преобразуем в DocumentLineItem
                        var lines = linesRaw.Select(l => new DocumentLineItem
                        {
                            Продукт_id = l.id,
                            Артикул = l.Артикул,
                            Продукт = l.Наименование,
                            Единица_измерения = l.Единица_измерения,
                            Количество = l.Количество,
                            Цена = l.Цена,
                            Сумма = l.Сумма ?? 0m
                        }).ToList();

                        _lines = lines;
                        ProductsGrid.ItemsSource = _lines;
                        UpdateTotalSum();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки документа: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ProductCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ProductCombo.SelectedItem != null)
            {
                dynamic selected = ProductCombo.SelectedItem;
                UnitText.Text = selected.Единица_измерения;
                decimal price = selected.Цена;
                PriceInfoText.Text = $"Цена: {price:N2} руб.";
            }
        }

        private void AddProductButton_Click(object sender, RoutedEventArgs e)
        {
            if (ProductCombo.SelectedItem == null)
            {
                MessageBox.Show("Выберите продукт", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!decimal.TryParse(QuantityTextBox.Text, out decimal quantity) || quantity <= 0)
            {
                MessageBox.Show("Введите корректное количество", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            dynamic selected = ProductCombo.SelectedItem;
            int productId = selected.id;
            string displayText = selected.DisplayText;
            string unit = selected.Единица_измерения;
            decimal price = selected.Цена;

            string[] parts = displayText.Split(new[] { " - " }, StringSplitOptions.None);
            string artikul = parts.Length > 0 ? parts[0] : "";
            string productName = parts.Length > 1 ? parts[1] : displayText;

            // Проверка на дубликат
            var existing = _lines.FirstOrDefault(l => l.Продукт_id == productId);
            if (existing != null)
            {
                existing.Количество += quantity;
                existing.Сумма = existing.Количество * existing.Цена;
            }
            else
            {
                _lines.Add(new DocumentLineItem
                {
                    Продукт_id = productId,
                    Артикул = artikul,
                    Продукт = productName,
                    Единица_измерения = unit,
                    Количество = quantity,
                    Цена = price,
                    Сумма = quantity * price
                });
            }

            ProductsGrid.ItemsSource = null;
            ProductsGrid.ItemsSource = _lines;
            UpdateTotalSum();

            QuantityTextBox.Text = "";
            ProductCombo.SelectedIndex = -1;
            UnitText.Text = "";
            PriceInfoText.Text = "";
        }

        private void DeleteProductButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var line = button?.DataContext as DocumentLineItem;
            if (line != null)
            {
                _lines.Remove(line);
                ProductsGrid.ItemsSource = null;
                ProductsGrid.ItemsSource = _lines;
                UpdateTotalSum();
            }
        }

        private void UpdateTotalSum()
        {
            decimal total = _lines.Sum(l => l.Сумма);
            TotalSumText.Text = $"{total:N2} руб.";
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (WarehouseCombo.SelectedValue == null)
            {
                MessageBox.Show("Выберите склад", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!_lines.Any())
            {
                MessageBox.Show("Добавьте хотя бы один продукт", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Для расхода проверяем остатки
            if (_documentType == "Расход")
            {
                using (var db = new MenuStolovayaDBEntities())
                {
                    int warehouseId = (int)WarehouseCombo.SelectedValue;
                    foreach (var line in _lines)
                    {
                        var stock = db.Остатки_продуктов
                            .FirstOrDefault(o => o.Склад_id == warehouseId && o.Продукт_id == line.Продукт_id);

                        decimal available = stock?.Количество ?? 0m;
                        if (available < line.Количество)
                        {
                            MessageBox.Show($"Недостаточно продукта \"{line.Продукт}\" на складе.\n" +
                                           $"Доступно: {available:F3}, требуется: {line.Количество:F3}",
                                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }
                    }
                }
            }

            try
            {
                using (var db = new MenuStolovayaDBEntities())
                {
                    Документы document;

                    if (_documentId.HasValue)
                    {
                        document = db.Документы.Find(_documentId.Value);
                        if (document == null)
                        {
                            MessageBox.Show("Документ не найден", "Ошибка",
                                MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }

                        // Удаляем старые строки
                        var oldLines = db.Строки_документов.Where(sd => sd.Документ_id == document.id).ToList();
                        foreach (var oldLine in oldLines)
                        {
                            db.Строки_документов.Remove(oldLine);
                        }
                    }
                    else
                    {
                        document = new Документы
                        {
                            Номер = NumberText.Text,
                            Тип_документа = _documentType,
                            Дата_создания = DateTime.Now,
                            Статус = "Черновик",
                            Кто_создал_id = ThisUser.CurrentUser.Id
                        };
                        db.Документы.Add(document);
                        db.SaveChanges(); // Сохраняем, чтобы получить ID
                    }

                    // Обновляем основные поля
                    document.Дата_документа = Date;
                    if (_documentType == "Приход")
                        document.Склад_получатель_id = (int)WarehouseCombo.SelectedValue;
                    else
                        document.Склад_отправитель_id = (int)WarehouseCombo.SelectedValue;

                    db.SaveChanges();

                    // Добавляем новые строки
                    foreach (var line in _lines)
                    {
                        db.Строки_документов.Add(new Строки_документов
                        {
                            Документ_id = document.id,
                            Продукт_id = line.Продукт_id,
                            Количество = line.Количество,
                            Цена = line.Цена
                        });
                    }

                    db.SaveChanges();
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
                }
            }
        }

        private void DatePicker_SelectedDateChanged(object sender, SelectionChangedEventArgs e) { }
        private void WarehouseCombo_SelectionChanged(object sender, SelectionChangedEventArgs e) { }
    }

    public class DocumentLineItem
    {
        public int Продукт_id { get; set; }
        public string Артикул { get; set; }
        public string Продукт { get; set; }
        public string Единица_измерения { get; set; }
        public decimal Количество { get; set; }
        public decimal Цена { get; set; }
        public decimal Сумма { get; set; }
    }
}