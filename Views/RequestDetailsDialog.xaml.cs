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
using System.IO;
using System.Diagnostics;
using MenuStolovaya.Models;

namespace MenuStolovaya.Views
{
    public partial class RequestDetailsDialog : Window
    {
        private int _requestId;

        public RequestDetailsDialog(int requestId)
        {
            InitializeComponent();
            _requestId = requestId;
            LoadRequestDetails();
        }

        private void LoadRequestDetails()
        {
            try
            {
                using (var db = new MenuStolovayaDBEntities())
                {
                    var request = db.vw_Требования_накладные
                        .FirstOrDefault(r => r.id == _requestId);

                    if (request != null)
                    {
                        TitleText.Text = $"Требование накладная - {request.Номер}";
                        NumberText.Text = request.Номер;
                        DateText.Text = request.Дата_документа.ToString("dd.MM.yyyy");
                        TechnologistText.Text = request.Технолог;
                        StatusText.Text = request.Статус_требования;
                        CommentText.Text = request.Комментарий ?? "";

                        if (StatusText.Text == "Подтверждено")
                            StatusText.Foreground = System.Windows.Media.Brushes.Green;
                        else if (StatusText.Text == "Ожидает")
                            StatusText.Foreground = System.Windows.Media.Brushes.Orange;
                        else
                            StatusText.Foreground = System.Windows.Media.Brushes.Red;
                    }

                    var details = db.vw_Требования_детали
                        .Where(d => d.Номер_требования == request.Номер)
                        .Select(d => new
                        {
                            d.Артикул,
                            d.Продукт,
                            d.Единица_измерения,
                            d.Количество,
                            d.Цена,
                            d.Сумма,
                            d.Остаток_на_складе
                        })
                        .ToList();

                    ProductsGrid.ItemsSource = details;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки деталей: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void PrintButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string htmlContent = GenerateRequestPrintHtml();
                string tempFile = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"Print_{DateTime.Now:yyyyMMddHHmmss}.html");
                File.WriteAllText(tempFile, htmlContent, Encoding.UTF8);

                // Просто открываем в браузере (без print verb)
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

        private string GenerateRequestPrintHtml()
        {
            using (var db = new MenuStolovayaDBEntities())
            {
                var request = db.vw_Требования_накладные
                    .FirstOrDefault(r => r.id == _requestId);

                if (request == null) return "<html><body><h1>Требование не найдено</h1></body></html>";

                var details = db.vw_Требования_детали
                    .Where(d => d.Номер_требования == request.Номер)
                    .ToList();

                decimal totalSum = (decimal)details.Sum(d => d.Сумма);

                StringBuilder html = new StringBuilder();
                html.AppendLine("<!DOCTYPE html>");
                html.AppendLine("<html lang='ru'>");
                html.AppendLine("<head>");
                html.AppendLine("    <meta charset='UTF-8'>");
                html.AppendLine("    <title>Требование накладная</title>");
                html.AppendLine("    <style>");
                html.AppendLine("        body { font-family: 'Segoe UI', Arial, sans-serif; margin: 20px; }");
                html.AppendLine("        .container { max-width: 1000px; margin: 0 auto; background: white; padding: 20px; }");
                html.AppendLine("        h1 { color: #2e7d32; border-bottom: 2px solid #4caf50; padding-bottom: 10px; }");
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
                html.AppendLine($"    <h1>Требование накладная №{request.Номер}</h1>");
                html.AppendLine("    <div class='info-grid'>");
                html.AppendLine($"        <div class='info-label'>Дата:</div><div>{request.Дата_документа:dd.MM.yyyy}</div>");
                html.AppendLine($"        <div class='info-label'>Меню от:</div><div>{request.Дата_меню:dd.MM.yyyy}</div>");
                html.AppendLine($"        <div class='info-label'>Технолог:</div><div>{request.Технолог}</div>");
                html.AppendLine($"        <div class='info-label'>Статус:</div><div>{request.Статус_требования}</div>");
                html.AppendLine($"        <div class='info-label'>Комментарий:</div><div>{request.Комментарий ?? ""}</div>");
                html.AppendLine("    </div>");
                html.AppendLine("    <h2>Состав требования</h2>");
                html.AppendLine("    <table>");
                html.AppendLine("        <thead>");
                html.AppendLine("            <tr><th>№</th><th>Артикул</th><th>Продукт</th><th>Ед. изм.</th><th>Количество</th><th>Цена</th><th>Сумма</th></tr>");
                html.AppendLine("        </thead>");
                html.AppendLine("        <tbody>");

                int index = 1;
                foreach (var d in details)
                {
                    html.AppendLine($"            <tr><td>{index++}</td><td>{d.Артикул}</td><td>{d.Продукт}</td><td>{d.Единица_измерения}</td><td>{d.Количество:F3}</td><td>{d.Цена:N2} руб.</td><td>{d.Сумма:N2} руб.</td></tr>");
                }

                html.AppendLine($"            <tr class='total-row'><td colspan='6' style='text-align: right;'><strong>Итого:</strong></td><td><strong>{totalSum:N2} руб.</strong></td></tr>");
                html.AppendLine("        </tbody>");
                html.AppendLine("    </table>");
                html.AppendLine("    <div class='footer'>");
                html.AppendLine($"        <div>© {DateTime.Now.Year} Меню столовой. Все права защищены.</div>");
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
            Close();
        }
    }
}