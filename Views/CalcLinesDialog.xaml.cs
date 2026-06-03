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
                html.AppendLine("        .container { max-width: 1100px; margin: 0 auto; background: white; padding: 20px; }");
                html.AppendLine("        .approval-header {");
                html.AppendLine("            border-bottom: 1px solid #ddd;");
                html.AppendLine("            margin-bottom: 20px;");
                html.AppendLine("            padding-bottom: 15px;");
                html.AppendLine("            text-align: right;");
                html.AppendLine("        }");
                html.AppendLine("        .approval-header .approval-block {");
                html.AppendLine("            display: inline-block;");
                html.AppendLine("            text-align: center;");
                html.AppendLine("            font-size: 11px;");
                html.AppendLine("            border: 1px solid #ccc;");
                html.AppendLine("            padding: 8px 15px;");
                html.AppendLine("            background: #f9f9f9;");
                html.AppendLine("            border-radius: 6px;");
                html.AppendLine("        }");
                html.AppendLine("        .approval-header .approval-block strong {");
                html.AppendLine("            font-size: 12px;");
                html.AppendLine("            display: block;");
                html.AppendLine("            margin-bottom: 4px;");
                html.AppendLine("        }");
                html.AppendLine("        h1 { color: #2e7d32; border-bottom: 2px solid #4caf50; padding-bottom: 10px; margin-top: 0; font-size: 22px; }");
                html.AppendLine("        h2 { color: #333; margin-top: 20px; }");
                html.AppendLine("        .info-grid { display: grid; grid-template-columns: 150px 1fr; gap: 10px; margin: 20px 0; }");
                html.AppendLine("        .info-label { font-weight: bold; }");
                html.AppendLine("        table { width: 100%; border-collapse: collapse; margin: 15px 0; }");
                html.AppendLine("        th, td { border: 1px solid #ddd; padding: 8px; text-align: left; }");
                html.AppendLine("        th { background-color: #4caf50; color: white; }");
                html.AppendLine("        .total-row { font-weight: bold; background-color: #f0f8ff; }");
                html.AppendLine("        .signatures {");
                html.AppendLine("            display: flex;");
                html.AppendLine("            justify-content: space-between;");
                html.AppendLine("            margin-top: 40px;");
                html.AppendLine("            padding-top: 20px;");
                html.AppendLine("            border-top: 1px solid #ddd;");
                html.AppendLine("        }");
                html.AppendLine("        .signature-item {");
                html.AppendLine("            text-align: center;");
                html.AppendLine("            width: 45%;");
                html.AppendLine("        }");
                html.AppendLine("        .signature-line {");
                html.AppendLine("            margin-top: 40px;");
                html.AppendLine("            border-top: 1px solid #000;");
                html.AppendLine("            width: 80%;");
                html.AppendLine("            margin-left: auto;");
                html.AppendLine("            margin-right: auto;");
                html.AppendLine("        }");
                html.AppendLine("        .signature-name {");
                html.AppendLine("            margin-top: 8px;");
                html.AppendLine("            font-size: 12px;");
                html.AppendLine("            color: #666;");
                html.AppendLine("        }");
                html.AppendLine("        .footer { margin-top: 30px; text-align: center; font-size: 12px; color: #666; border-top: 1px solid #ddd; padding-top: 15px; }");
                html.AppendLine("        @media print { body { margin: 0; } .no-print { display: none; } }");
                html.AppendLine("    </style>");
                html.AppendLine("</head>");
                html.AppendLine("<body>");
                html.AppendLine("<div class='container'>");

                // Блок утверждения над заголовком
                html.AppendLine("    <div class='approval-header'>");
                html.AppendLine("        <div class='approval-block'>");
                html.AppendLine("            <strong>УТВЕРЖДАЮ</strong>");
                html.AppendLine("            <div>Директор ООО «СФ „Белка-Фаворит“»</div>");
                html.AppendLine("            <div style='margin-top: 8px;'>_______________ /_______________________/</div>");
                html.AppendLine("            <div style='font-size: 9px;'>подпись                         ФИО</div>");
                html.AppendLine("            <div style='margin-top: 3px;'>«__» _________ 20___ г.</div>");
                html.AppendLine("        </div>");
                html.AppendLine("    </div>");

                html.AppendLine($"    <h1>Калькуляционная карточка №{calcCard.Номер}</h1>");
                html.AppendLine("    <div class='info-grid'>");
                html.AppendLine($"        <div class='info-label'>Блюдо:</div><div>{calcCard.Технологические_карты?.Блюда?.Наименование ?? "Неизвестно"}</div>");
                html.AppendLine($"        <div class='info-label'>Выход:</div><div>{(calcCard.Технологические_карты?.Выход ?? 0):N1} г</div>");
                html.AppendLine($"        <div class='info-label'>Дата составления:</div><div>{calcCard.Дата_составления:dd.MM.yyyy}</div>");
                html.AppendLine($"        <div class='info-label'>Наценка:</div><div>{calcCard.Процент_наценки ?? 0:F0}%</div>");
                html.AppendLine("    </div>");
                html.AppendLine("    <h2>Расчёт себестоимости</h2>");

                // Открываем таблицу
                html.AppendLine("    <table>");
                html.AppendLine("        <thead>");
                html.AppendLine("            <tr>");
                html.AppendLine("                <th>№</th>");
                html.AppendLine("                <th>Артикул</th>");
                html.AppendLine("                <th>Продукт</th>");
                html.AppendLine("                <th>Ед. изм.</th>");
                html.AppendLine("                <th>Норма расхода (кг)</th>");
                html.AppendLine("                <th>Цена за ед.</th>");
                html.AppendLine("                <th>Сумма</th>");
                html.AppendLine("            </tr>");
                html.AppendLine("        </thead>");
                html.AppendLine("        <tbody>");

                int index = 1;
                foreach (var line in calcLines)
                {
                    html.AppendLine("            <tr>");
                    html.AppendLine($"                <td>{index++}</td>");
                    html.AppendLine($"                <td>{line.Артикул ?? ""}</td>");
                    html.AppendLine($"                <td>{line.Продукт}</td>");
                    html.AppendLine($"                <td>{line.Единица_измерения}</td>");
                    html.AppendLine($"                <td>{line.Норма_расхода:F3}</td>");
                    html.AppendLine($"                <td>{line.Цена_за_единицу:N2} руб.</td>");
                    html.AppendLine($"                <td>{line.Сумма:N2} руб.</td>");
                    html.AppendLine("            </tr>");
                }

                // Итоговые строки
                html.AppendLine("            <tr class='total-row'>");
                html.AppendLine("                <td colspan='6' style='text-align: right;'><strong>Итого себестоимость:</strong></td>");
                html.AppendLine($"                <td><strong>{totalCost:N2} руб.</strong></td>");
                html.AppendLine("            </tr>");
                html.AppendLine("            <tr class='total-row'>");
                html.AppendLine("                <td colspan='6' style='text-align: right;'><strong>Цена реализации:</strong></td>");
                html.AppendLine($"                <td><strong>{price:N2} руб.</strong></td>");
                html.AppendLine("            </tr>");
                html.AppendLine("            <tr class='total-row'>");
                html.AppendLine("                <td colspan='6' style='text-align: right;'><strong>Food Cost:</strong></td>");
                html.AppendLine($"                <td><strong>{foodCost:N2}%</strong></td>");
                html.AppendLine("            </tr>");

                html.AppendLine("        </tbody>");
                html.AppendLine("    </table>");

                // Подписи снизу
                html.AppendLine("    <div class='signatures'>");
                html.AppendLine("        <div class='signature-item'>");
                html.AppendLine("            <div class='signature-line'></div>");
                html.AppendLine("            <div class='signature-name'>Технолог / Заведующий производством</div>");
                html.AppendLine("            <div class='signature-name'>_______________ /_______________________/</div>");
                html.AppendLine("            <div class='signature-name' style='font-size: 10px;'>подпись                         ФИО</div>");
                html.AppendLine("        </div>");
                html.AppendLine("        <div class='signature-item'>");
                html.AppendLine("            <div class='signature-line'></div>");
                html.AppendLine("            <div class='signature-name'>Бухгалтер</div>");
                html.AppendLine("            <div class='signature-name'>_______________ /_______________________/</div>");
                html.AppendLine("            <div class='signature-name' style='font-size: 10px;'>подпись                         ФИО</div>");
                html.AppendLine("        </div>");
                html.AppendLine("    </div>");

                html.AppendLine("    <div class='footer'>");
                html.AppendLine($"        <div>© {DateTime.Now.Year} ООО «СФ „Белка-Фаворит“». Все права защищены.</div>");
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