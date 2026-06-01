using System;
using System.Linq;
using System.Windows;
using System.IO;
using System.Diagnostics;
using System.Text;
using MenuStolovaya.Models;
using MenuStolovaya.Services;

namespace MenuStolovaya.Views
{
    public partial class TechCardDetailsDialog : Window
    {
        private int _techCardId;
        private AccountantTechCardService _service;

        public TechCardDetailsDialog(int techCardId)
        {
            InitializeComponent();
            _techCardId = techCardId;
            _service = new AccountantTechCardService();
            LoadTechCardDetails();
            LoadRecipes();
        }

        private void LoadTechCardDetails()
        {
            try
            {
                var details = _service.GetTechCardDetails(_techCardId);
                if (details == null) return;

                TitleText.Text = $"Технологическая карта - {details.Номер}";

                CardNumberText.Text = details.Номер;
                DishText.Text = details.Блюдо;
                DishTypeText.Text = details.Вид_блюда;
                OutputText.Text = $"{details.Выход:N1} г";
                StatusText.Text = details.Статус;
                CreationDateText.Text = details.Дата_создания.ToString("dd.MM.yyyy HH:mm");

                TechnologyText.Text = details.Технология_приготовления ?? "Не указана";

                if (details.Статус == "Утверждена")
                {
                    StatusText.Foreground = System.Windows.Media.Brushes.Green;
                }
                else if (details.Статус == "Черновик")
                {
                    StatusText.Foreground = System.Windows.Media.Brushes.Orange;
                }
                else
                {
                    StatusText.Foreground = System.Windows.Media.Brushes.Red;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке деталей: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadRecipes()
        {
            try
            {
                using (var db = new MenuStolovayaDBEntities())
                {
                    var recipes = db.Рецептуры
                        .Where(r => r.Технологическая_карта_id == _techCardId)
                        .Join(db.Продукты,
                            r => r.Продукт_id,
                            p => p.id,
                            (r, p) => new RecipeDisplayForDialog
                            {
                                Порядок_закладки = r.Порядок_закладки ?? 0,
                                Артикул = p.Артикул,
                                Продукт = p.Наименование,
                                Единица_измерения = p.Единица_измерения,
                                Количество_брутто = r.Количество_брутто,
                                Цена = p.Цена ?? 0,
                                Сумма = (r.Количество_нетто ?? r.Количество_брутто) * (p.Цена ?? 0)
                            })
                        .OrderBy(r => r.Порядок_закладки)
                        .ToList();

                    RecipesGrid.ItemsSource = recipes;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке рецептуры: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void PrintButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string htmlContent = GenerateTechCardPrintHtml();
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

        private string GenerateTechCardPrintHtml()
        {
            using (var db = new MenuStolovayaDBEntities())
            {
                var details = _service.GetTechCardDetails(_techCardId);
                if (details == null) return "<html><body><h1>Карта не найдена</h1></body></html>";

                // Получаем рецептуру с ценами
                var recipes = db.Рецептуры
                    .Where(r => r.Технологическая_карта_id == _techCardId)
                    .Join(db.Продукты,
                        r => r.Продукт_id,
                        p => p.id,
                        (r, p) => new
                        {
                            Артикул = p.Артикул,
                            Продукт = p.Наименование,
                            Единица_измерения = p.Единица_измерения,
                            Количество_брутто = r.Количество_брутто,
                            Количество_нетто = r.Количество_нетто ?? r.Количество_брутто,
                            Потери_холодной = p.Потери_холодной_обработки ?? 0,
                            Потери_горячей = p.Потери_горячей_обработки ?? 0,
                            Цена = p.Цена ?? 0,
                            Сумма = (r.Количество_нетто ?? r.Количество_брутто) * (p.Цена ?? 0)
                        })
                    .ToList();

                // Получаем калорийность блюда из таблицы Блюда
                var dish = db.Блюда.FirstOrDefault(b => b.Наименование == details.Блюдо);
                decimal caloriesPer100g = dish?.Калорийность_расчетная ?? 0;

                decimal totalCost = recipes.Sum(r => r.Сумма);

                StringBuilder html = new StringBuilder();
                html.AppendLine("<!DOCTYPE html>");
                html.AppendLine("<html lang='ru'>");
                html.AppendLine("<head>");
                html.AppendLine("    <meta charset='UTF-8'>");
                html.AppendLine("    <title>Технологическая карта</title>");
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
                html.AppendLine($"    <h1>Технологическая карта №{details.Номер}</h1>");
                html.AppendLine("    <div class='info-grid'>");
                html.AppendLine($"        <div class='info-label'>Блюдо:</div><div>{details.Блюдо}</div>");
                html.AppendLine($"        <div class='info-label'>Вид блюда:</div><div>{details.Вид_блюда}</div>");
                html.AppendLine($"        <div class='info-label'>Выход:</div><div>{details.Выход:N1} г</div>");
                html.AppendLine($"        <div class='info-label'>Калорийность:</div><div>{caloriesPer100g:F1} ккал/100г</div>");
                html.AppendLine($"        <div class='info-label'>Статус:</div><div>{details.Статус}</div>");
                html.AppendLine($"        <div class='info-label'>Дата создания:</div><div>{details.Дата_создания:dd.MM.yyyy}</div>");
                html.AppendLine("    </div>");
                html.AppendLine("    <h2>Рецептура</h2>");
                html.AppendLine("    <table>");
                html.AppendLine("        <thead>");
                html.AppendLine("            <tr><th>№</th><th>Артикул</th><th>Продукт</th><th>Ед. изм.</th><th>Кол-во брутто (кг)</th><th>Потери холод.</th><th>Потери гор.</th><th>Кол-во нетто (кг)</th><th>Цена (руб/кг)</th><th>Сумма (руб)</th></tr>");
                html.AppendLine("        </thead>");
                html.AppendLine("        <tbody>");

                int index = 1;
                foreach (var r in recipes)
                {
                    html.AppendLine($"            <tr><td style='text-align:center'>{index++}</td>");
                    html.AppendLine($"            <td>{r.Артикул ?? ""}</td>");
                    html.AppendLine($"            <td>{r.Продукт}</td>");
                    html.AppendLine($"            <td>{r.Единица_измерения}</td>");
                    html.AppendLine($"            <td>{r.Количество_брутто:F3}</td>");
                    html.AppendLine($"            <td>{r.Потери_холодной:F1}%</td>");
                    html.AppendLine($"            <td>{r.Потери_горячей:F1}%</td>");
                    html.AppendLine($"            <td>{r.Количество_нетто:F3}</td>");
                    html.AppendLine($"            <td>{r.Цена:N2}</td>");
                    html.AppendLine($"            <td>{r.Сумма:N2}</td>");
                    html.AppendLine("            </tr>");
                }

                html.AppendLine($"            <tr class='total-row'><td colspan='9' style='text-align: right;'><strong>Общая себестоимость:</strong></td><td><strong>{totalCost:N2} руб.</strong></td></tr>");
                html.AppendLine("        </tbody>");
                html.AppendLine("    </table>");
                html.AppendLine("    <h2>Технология приготовления</h2>");
                html.AppendLine($"    <p>{details.Технология_приготовления ?? "Не указана"}</p>");
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
            this.Close();
        }

        public class RecipeDisplayForDialog
        {
            public int Порядок_закладки { get; set; }
            public string Артикул { get; set; }
            public string Продукт { get; set; }
            public string Единица_измерения { get; set; }
            public decimal Количество_брутто { get; set; }
            public decimal Цена { get; set; }
            public decimal Сумма { get; set; }
        }
    }
}