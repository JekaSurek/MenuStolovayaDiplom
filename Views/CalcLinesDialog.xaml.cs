using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using MenuStolovaya.Models;

namespace MenuStolovaya.Views
{
    public partial class CalcLinesDialog : Window
    {
        private int _calcCardId;

        public CalcLinesDialog(int calcCardId)
        {
            InitializeComponent();
            _calcCardId = calcCardId;
            LoadCalcLines();
        }

        private void LoadCalcLines()
        {
            try
            {
                using (var db = new MenuStolovayaDBEntities())
                {
                    var calcCard = db.Калькуляционные_карточки
                        .Include("Технологические_карты")
                        .Include("Технологические_карты.Блюда")
                        .FirstOrDefault(cc => cc.id == _calcCardId);

                    if (calcCard == null) return;

                    // Заголовок
                    TitleText.Text = $"Строки калькуляции - {calcCard.Номер}";

                    // Информация о блюде
                    DishInfoText.Text = $"Блюдо: {calcCard.Технологические_карты?.Блюда?.Наименование ?? "Неизвестно"}";
                    OutputText.Text = $"Выход: {(calcCard.Технологические_карты?.Выход ?? 0):N1} г";

                    // Загружаем строки калькуляции
                    var calcLines = db.Строки_калькуляции
                        .Where(cl => cl.Калькуляционная_карточка_id == _calcCardId)
                        .Join(db.Продукты,
                            cl => cl.Продукт_id,
                            p => p.id,
                            (cl, p) => new
                            {
                                Продукт = p.Наименование,
                                Единица_измерения = p.Единица_измерения,
                                Норма_расхода = cl.Норма_расхода,
                                Цена_за_единицу = cl.Цена_за_единицу,
                                Сумма = cl.Сумма
                            })
                        .OrderBy(cl => cl.Продукт)
                        .ToList();

                    // Преобразуем в список для DataGrid
                    var displayLines = new List<CalcLineDisplayItem>();
                    for (int i = 0; i < calcLines.Count; i++)
                    {
                        var line = calcLines[i];
                        displayLines.Add(new CalcLineDisplayItem
                        {
                            RowNumber = i + 1,
                            Продукт = line.Продукт,
                            Единица_измерения = line.Единица_измерения,
                            Норма_расхода = line.Норма_расхода,
                            Цена_за_единицу = line.Цена_за_единицу,
                            Сумма = line.Сумма
                        });
                    }

                    CalcLinesGrid.ItemsSource = displayLines;

                    // Рассчитываем итоги
                    decimal totalCost = calcLines.Sum(cl => cl.Сумма);
                    decimal price = calcCard.Цена_реализации ?? 0;
                    decimal foodCost = price > 0 ? (totalCost / price) * 100 : 0;

                    TotalCostText.Text = $"{totalCost:N2} руб.";
                    TotalPriceText.Text = $"{price:N2} руб.";
                    FoodCostText.Text = $"Food Cost: {foodCost:N2}%";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке строк калькуляции: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void PrintButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var printDialog = new System.Windows.Controls.PrintDialog();
                if (printDialog.ShowDialog() == true)
                {
                    printDialog.PrintVisual(this, $"Калькуляционная карточка {_calcCardId}");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при печати: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }

    // Локальный класс для отображения в DataGrid
    public class CalcLineDisplayItem
    {
        public int RowNumber { get; set; }
        public string Продукт { get; set; }
        public string Единица_измерения { get; set; }
        public decimal Норма_расхода { get; set; }
        public decimal Цена_за_единицу { get; set; }
        public decimal Сумма { get; set; }
    }
}