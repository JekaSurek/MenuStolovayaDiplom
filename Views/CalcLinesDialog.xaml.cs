using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.IO;
using System.Diagnostics;
using System.Text;
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

                    TitleText.Text = $"Строки калькуляции - {calcCard.Номер}";

                    DishInfoText.Text = $"Блюдо: {calcCard.Технологические_карты?.Блюда?.Наименование ?? "Неизвестно"}";
                    OutputText.Text = $"Выход: {(calcCard.Технологические_карты?.Выход ?? 0):N1} г";

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
                string htmlContent = GenerateCalcCardPrintHtml();
                string tempFile = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"Print_{DateTime.Now:yyyyMMddHHmmss}.html");
                File.WriteAllText(tempFile, htmlContent, Encoding.UTF8);

                // Просто открываем в браузере
                var psi = new ProcessStartInfo
                {
                    FileName = tempFile,
                    UseShellExecute = true
                };
                Process.Start(psi);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при открытии: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private string GenerateCalcCardPrintHtml()
        {
            using (var db = new MenuStolovayaDBEntities())
            {
                var calcCard = db.Калькуляционные_карточки
                    .Include("Технологические_карты")
                    .Include("Технологические_карты.Блюда")
                    .FirstOrDefault(cc => cc.id == _calcCardId);

                if (calcCard == null) return "<html><body><h1>Карточка не найдена</h1></body></html>";

                var calcLines = db.Строки_калькуляции
                    .Where(cl => cl.Калькуляционная_карточка_id == _calcCardId)
                    .Join(db.Продукты,
                        cl => cl.Продукт_id,
                        p => p.id,
                        (cl, p) => new
                        {
                            Артикул = p.Артикул,
                            Продукт = p.Наименование,
                            Единица_измерения = p.Единица_измерения,
                            Норма_расхода = cl.Норма_расхода,
                            Цена_за_единицу = cl.Цена_за_единицу,
                            Сумма = cl.Сумма
                        })
                    .ToList();

                decimal totalCost = calcLines.Sum(cl => cl.Сумма);
                decimal price = calcCard.Цена_реализации ?? 0;
                decimal foodCost = price > 0 ? (totalCost / price) * 100 : 0;

                StringBuilder html = new StringBuilder();
                html.AppendLine("<!DOCTYPE html>");
                html.AppendLine("<html lang='ru'>");
                html.AppendLine("<head>");
                html.AppendLine("    <meta charset='UTF-8'>");
                html.AppendLine("    <title>Калькуляционная карточка</title>");
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
                html.AppendLine("        .total-row { font-weight: bold; background-color: #f0f8ff; }");
                html.AppendLine("        .footer { margin-top: 30px; text-align: center; font-size: 12px; color: #666; border-top: 1px solid #ddd; padding-top: 15px; }");
                html.AppendLine("        @media print { body { margin: 0; } .no-print { display: none; } }");
                html.AppendLine("    </style>");
                html.AppendLine("</head>");
                html.AppendLine("<body>");
                html.AppendLine("<div class='container'>");
                html.AppendLine($"    <h1>Калькуляционная карточка №{calcCard.Номер}</h1>");
                html.AppendLine("    <div class='info-grid'>");
                html.AppendLine($"        <div class='info-label'>Блюдо:</div><div>{calcCard.Технологические_карты?.Блюда?.Наименование ?? "Неизвестно"}</div>");
                html.AppendLine($"        <div class='info-label'>Выход:</div><div>{(calcCard.Технологические_карты?.Выход ?? 0):N1} г</div>");
                html.AppendLine($"        <div class='info-label'>Дата составления:</div><div>{calcCard.Дата_составления:dd.MM.yyyy}</div>");
                html.AppendLine($"        <div class='info-label'>Наценка:</div><div>{calcCard.Процент_наценки ?? 0:F0}%</div>");
                html.AppendLine($"        <div class='info-label'>Статус:</div><div>{calcCard.Статус}</div>");
                html.AppendLine("    </div>");
                html.AppendLine("    <h2>Расчёт себестоимости</h2>");
                html.AppendLine("    <table>");
                html.AppendLine("        <thead>");
                html.AppendLine("            <tr><th>№</th><th>Артикул</th><th>Продукт</th><th>Ед. изм.</th><th>Норма расхода (кг)</th><th>Цена за ед.</th><th>Сумма</th></tr>");
                html.AppendLine("        </thead>");
                html.AppendLine("        <tbody>");

                int index = 1;
                foreach (var line in calcLines)
                {
                    html.AppendLine($"            <tr><td>{index++}</td><td>{line.Артикул}</td><td>{line.Продукт}</td><td>{line.Единица_измерения}</td><td>{line.Норма_расхода:F3}</td><td>{line.Цена_за_единицу:N2} руб.</td><td>{line.Сумма:N2} руб.</td></tr>");
                }

                html.AppendLine($"            <tr class='total-row'><td colspan='6' style='text-align: right;'><strong>Итого себестоимость:</strong></td><td><strong>{totalCost:N2} руб.</strong></td></tr>");
                html.AppendLine($"            <tr class='total-row'><td colspan='6' style='text-align: right;'><strong>Цена реализации:</strong></td><td><strong>{price:N2} руб.</strong></td></tr>");
                html.AppendLine($"            <tr class='total-row'><td colspan='6' style='text-align: right;'><strong>Food Cost:</strong></td><td><strong>{foodCost:N2}%</strong></td></tr>");
                html.AppendLine("        </tbody>");
                html.AppendLine("    </table>");
                html.AppendLine("    <div class='footer'>");
                html.AppendLine($"        <div>© {DateTime.Now.Year} MenuStolovaya. Все права защищены.</div>");
                html.AppendLine("        <button class='no-print' onclick='window.print()'>🖨️ Распечатать</button>");
                html.AppendLine("    </div>");
                html.AppendLine("</div>");
                html.AppendLine("</body>");
                html.AppendLine("</html>");

                return html.ToString();
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }

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