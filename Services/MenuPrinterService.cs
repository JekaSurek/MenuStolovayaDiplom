using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Windows;
using MenuStolovaya.Models;

namespace MenuStolovaya.Services
{
    public class MenuPrinterService
    {
        public string GenerateMenuHtml(int menuId)
        {
            try
            {
                using (var db = new MenuStolovayaDBEntities())
                {
                    var menu = db.Меню_на_день
                        .Include("Пользователи")
                        .FirstOrDefault(m => m.id == menuId);

                    if (menu == null)
                        return "<html><body><h1>Меню не найдено</h1></body></html>";

                    var menuItems = db.Строки_меню
                        .Where(sm => sm.Меню_id == menuId)
                        .Join(db.Блюда,
                            sm => sm.Блюдо_id,
                            b => b.id,
                            (sm, b) => new
                            {
                                Id = sm.id,
                                Блюдо = b.Наименование,
                                Полное_наименование = b.Полное_наименование,
                                Вид_блюда = b.Виды_блюд != null ? b.Виды_блюд.Наименование : "Не указано",
                                Количество_порций = sm.Количество_порций ?? 1,
                                Выход_на_порцию = sm.Выход_на_порцию ?? b.Выход_стандартный ?? 100,
                                Время_подачи = sm.Время_подачи ?? "Обед",
                                Порядок_подачи = sm.Порядок_подачи ?? 0,
                                Калорийность_расчетная = b.Калорийность_расчетная ?? 0,
                                Блюдо_id = b.id // Добавлено
                            })
                        .ToList()
                        .Select(x => new MenuItemWithDetails
                        {
                            Id = x.Id,
                            Блюдо = x.Блюдо,
                            Полное_наименование = x.Полное_наименование,
                            Вид_блюда = x.Вид_блюда,
                            Количество_порций = x.Количество_порций,
                            Выход_на_порцию = x.Выход_на_порцию,
                            Время_подачи = x.Время_подачи,
                            Порядок_подачи = x.Порядок_подачи,
                            Калорийность_расчетная = x.Калорийность_расчетная,
                            Цена = GetDishPrice(x.Блюдо_id), // Добавлено вычисление цены
                            Блюдо_id = x.Блюдо_id // Добавлено
                        })
                        .OrderBy(m => m.Время_подачи)
                        .ThenBy(m => m.Порядок_подачи)
                        .ToList();

                    // Группируем по времени подачи
                    var timeGroups = menuItems
                        .GroupBy(m => m.Время_подачи)
                        .OrderBy(g => GetTimeOrder(g.Key));

                    // Генерируем HTML
                    StringBuilder html = new StringBuilder();

                    // Начало HTML
                    html.AppendLine("<!DOCTYPE html>");
                    html.AppendLine("<html lang='ru'>");
                    html.AppendLine("<head>");
                    html.AppendLine("    <meta charset='UTF-8'>");
                    html.AppendLine("    <meta name='viewport' content='width=device-width, initial-scale=1.0'>");
                    html.AppendLine("    <title>Меню столовой</title>");
                    html.AppendLine("    <style>");
                    html.AppendLine("        body { font-family: 'Arial', sans-serif; margin: 20px; color: #333; }");
                    html.AppendLine("        .header { text-align: center; margin-bottom: 30px; border-bottom: 2px solid #4CAF50; padding-bottom: 10px; }");
                    html.AppendLine("        .header h1 { color: #4CAF50; margin: 0; }");
                    html.AppendLine("        .header .date { font-size: 18px; color: #666; margin-top: 5px; }");
                    html.AppendLine("        .section { margin-bottom: 30px; }");
                    html.AppendLine("        .section-title { background-color: #4CAF50; color: white; padding: 10px 15px; border-radius: 5px; margin-bottom: 15px; font-size: 20px; }");
                    html.AppendLine("        .dish-category { background-color: #f5f5f5; padding: 10px; border-left: 4px solid #2196F3; margin-bottom: 15px; }");
                    html.AppendLine("        .category-title { font-weight: bold; color: #2196F3; margin-bottom: 5px; font-size: 16px; }");
                    html.AppendLine("        .dish-table { width: 100%; border-collapse: collapse; margin-top: 10px; }");
                    html.AppendLine("        .dish-table th { background-color: #f8f9fa; text-align: left; padding: 10px; border-bottom: 2px solid #dee2e6; }");
                    html.AppendLine("        .dish-table td { padding: 10px; border-bottom: 1px solid #dee2e6; vertical-align: top; }");
                    html.AppendLine("        .dish-table tr:hover { background-color: #f8f9fa; }");
                    html.AppendLine("        .dish-name { font-weight: bold; color: #2c3e50; }");
                    html.AppendLine("        .dish-description { font-style: italic; color: #666; font-size: 14px; margin-top: 3px; }");
                    html.AppendLine("        .dish-details { font-size: 14px; color: #555; }");
                    html.AppendLine("        .price { font-weight: bold; color: #e74c3c; text-align: right; }");
                    html.AppendLine("        .nutrients { background-color: #e8f5e8; padding: 8px; border-radius: 4px; margin-top: 5px; font-size: 12px; }");
                    html.AppendLine("        .footer { margin-top: 40px; padding-top: 20px; border-top: 1px solid #ddd; text-align: center; color: #666; font-size: 14px; }");
                    html.AppendLine("        .calories-total { font-weight: bold; color: #2c3e50; margin-top: 10px; padding: 10px; background-color: #f0f8ff; border-radius: 5px; }");
                    html.AppendLine("        .allergy-info { background-color: #fff3cd; border: 1px solid #ffeaa7; padding: 10px; border-radius: 4px; margin-top: 20px; font-size: 14px; }");
                    html.AppendLine("        @media print { body { margin: 0; padding: 20px; } .no-print { display: none; } }");
                    html.AppendLine("    </style>");
                    html.AppendLine("</head>");
                    html.AppendLine("<body>");

                    // Заголовок
                    html.AppendLine("    <div class='header'>");
                    html.AppendLine($"        <h1>🍽️ Меню столовой</h1>");
                    html.AppendLine($"        <div class='date'>На {menu.Дата:dd.MM.yyyy} ({GetDayOfWeek(menu.Дата)})</div>");
                    html.AppendLine($"        <div>Ответственный: {menu.Пользователи?.Фамилия} {menu.Пользователи?.Имя}</div>");
                    html.AppendLine($"        <div>Дата составления: {(menu.Дата_составления ?? DateTime.Now):dd.MM.yyyy HH:mm}</div>");
                    html.AppendLine("    </div>");

                    // Статистика меню
                    decimal totalCalories = menuItems.Sum(m =>
    (m.Калорийность_расчетная / 100m * m.Выход_на_порцию) * m.Количество_порций);
                    int totalPortions = menuItems.Sum(m => m.Количество_порций);

                    html.AppendLine("    <div class='calories-total'>");
                    html.AppendLine($"        <div>📊 Общая информация:</div>");
                    html.AppendLine($"        <div>• Количество блюд: {menuItems.Count}</div>");
                    html.AppendLine($"        <div>• Всего порций: {totalPortions}</div>");
                    html.AppendLine($"        <div>• Общая калорийность: {menu.Калорийность_общая ?? totalCalories:F0} калорий</div>");
                    html.AppendLine("    </div>");

                    // Группировка по времени подачи
                    foreach (var timeGroup in timeGroups)
                    {
                        html.AppendLine($"    <div class='section'>");
                        html.AppendLine($"        <div class='section-title'>⏰ {timeGroup.Key}</div>");

                        // Группируем блюда по виду
                        var dishTypeGroups = timeGroup
                            .GroupBy(m => m.Вид_блюда)
                            .OrderBy(g => g.Key);

                        foreach (var dishTypeGroup in dishTypeGroups)
                        {
                            html.AppendLine($"        <div class='dish-category'>");
                            html.AppendLine($"            <div class='category-title'>🍲 {dishTypeGroup.Key}</div>");

                            html.AppendLine("            <table class='dish-table'>");
                            html.AppendLine("                <thead>");
                            html.AppendLine("                    <tr>");
                            html.AppendLine("                        <th style='width: 40%'>Блюдо</th>");
                            html.AppendLine("                        <th style='width: 20%'>Порция</th>");
                            html.AppendLine("                        <th style='width: 20%'>КБЖУ</th>");
                            html.AppendLine("                        <th style='width: 20%' class='price'>Цена</th>");
                            html.AppendLine("                    </tr>");
                            html.AppendLine("                </thead>");
                            html.AppendLine("                <tbody>");

                            foreach (var dish in dishTypeGroup.OrderBy(d => d.Порядок_подачи))
                            {
                                // Получаем детали БЖУ
                                var nutrients = NutrientCalculator.CalculateDishNutrients(dish.Блюдо_id);

                                // Делим калории на 1000, т.к. CalculateDishNutrients возвращает калории, а не ккал
                                decimal caloriesFromNutrientsInKcal = nutrients.Калории / 1000m;

                                html.AppendLine("                    <tr>");
                                html.AppendLine($"                        <td>");
                                html.AppendLine($"                            <div class='dish-name'>{dish.Блюдо}</div>");
                                if (!string.IsNullOrEmpty(dish.Полное_наименование) && dish.Полное_наименование != dish.Блюдо)
                                {
                                    html.AppendLine($"                            <div class='dish-description'>{dish.Полное_наименование}</div>");
                                }
                                html.AppendLine($"                        </td>");
                                html.AppendLine($"                        <td class='dish-details'>");
                                html.AppendLine($"                            {dish.Выход_на_порцию:F0} г<br>");
                                html.AppendLine($"                            <small>{dish.Количество_порций} порц.</small>");
                                html.AppendLine($"                        </td>");
                                html.AppendLine($"                        <td class='dish-details'>");

                                // Калорийность на 100г - Уже хранится в dish.Калорийность_расчетная
                                decimal caloriesPer100g = dish.Калорийность_расчетная; // В ккал/100г

                                html.AppendLine($"                            {caloriesPer100g:F2} калорий/100г<br>");
                                if (nutrients.Белки > 0)
                                    html.AppendLine($"                            Б: {nutrients.Белки:F1}г<br>");
                                if (nutrients.Жиры > 0)
                                    html.AppendLine($"                            Ж: {nutrients.Жиры:F1}г<br>");
                                if (nutrients.Углеводы > 0)
                                    html.AppendLine($"                            У: {nutrients.Углеводы:F1}г");
                                html.AppendLine($"                        </td>");
                                html.AppendLine($"                        <td class='price'>{dish.Цена:F2} руб.</td>");
                                html.AppendLine("                    </tr>");
                            }

                            html.AppendLine("                </tbody>");
                            html.AppendLine("            </table>");
                            html.AppendLine("        </div>");
                        }

                        html.AppendLine("    </div>");
                    }

                    // Информация об аллергенах
                    html.AppendLine("    <div class='allergy-info'>");
                    html.AppendLine("        <strong>⚠️ Информация об аллергенах:</strong>");
                    html.AppendLine("        <p>При приготовлении блюд могут использоваться: молоко, яйца, глютен, орехи, соя, рыба и морепродукты.</p>");
                    html.AppendLine("        <p>При наличии аллергических реакций проконсультируйтесь с персоналом столовой.</p>");
                    html.AppendLine("    </div>");

                    // Подвал
                    html.AppendLine("    <div class='footer'>");
                    html.AppendLine($"        <p>Меню подготовлено: {menu.Пользователи?.Фамилия} {menu.Пользователи?.Имя}</p>");
                    html.AppendLine($"        <p>Телефон столовой:</p>");
                    html.AppendLine("        <button class='no-print' onclick='window.print()'>🖨️ Печать меню</button>");
                    html.AppendLine("    </div>");

                    html.AppendLine("</body>");
                    html.AppendLine("</html>");

                    return html.ToString();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при генерации HTML меню: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return "<html><body><h1>Ошибка при создании меню</h1></body></html>";
            }
        }

        private decimal GetDishPrice(int dishId)
        {
            try
            {
                using (var db = new MenuStolovayaDBEntities())
                {
                    var techCard = db.Технологические_карты
                        .FirstOrDefault(tc => tc.Блюдо_id == dishId);

                    if (techCard == null) return 0;

                    var calcCard = db.Калькуляционные_карточки
                        .FirstOrDefault(cc => cc.Технологическая_карта_id == techCard.id &&
                                              cc.Статус == "Утверждена");

                    return calcCard?.Цена_реализации ?? 0;
                }
            }
            catch
            {
                return 0;
            }
        }

        private string GetDayOfWeek(DateTime date)
        {
            switch (date.DayOfWeek)
            {
                case DayOfWeek.Monday: return "Понедельник";
                case DayOfWeek.Tuesday: return "Вторник";
                case DayOfWeek.Wednesday: return "Среда";
                case DayOfWeek.Thursday: return "Четверг";
                case DayOfWeek.Friday: return "Пятница";
                case DayOfWeek.Saturday: return "Суббота";
                case DayOfWeek.Sunday: return "Воскресенье";
                default: return "";
            }
        }

        private int GetTimeOrder(string time)
        {
            if (time == "Завтрак") return 1;
            if (time == "Обед") return 2;
            if (time == "Ужин") return 3;
            if (time == "Полдник") return 4;
            return 5;
        }

        public void SaveHtmlToFile(int menuId, string filePath)
        {
            try
            {
                string html = GenerateMenuHtml(menuId);
                File.WriteAllText(filePath, html, Encoding.UTF8);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении файла: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void ShowMenuInBrowser(int menuId)
        {
            try
            {
                string tempFile = Path.Combine(Path.GetTempPath(), $"menu_{menuId}_{DateTime.Now:yyyyMMddHHmmss}.html");
                SaveHtmlToFile(menuId, tempFile);

                // Открываем в браузере с указанием ширины и высоты
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = tempFile,
                    UseShellExecute = true,
                    //Этот параметр может помочь скрыть адресную строку в некоторых браузерах
                    Verb = "open"
                });

            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при открытии меню: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    public class MenuItemWithDetails
    {
        public int Id { get; set; }
        public string Блюдо { get; set; }
        public string Полное_наименование { get; set; }
        public string Вид_блюда { get; set; }
        public int Количество_порций { get; set; }
        public decimal Выход_на_порцию { get; set; }
        public string Время_подачи { get; set; }
        public int Порядок_подачи { get; set; }
        public decimal Калорийность_расчетная { get; set; }
        public decimal Цена { get; set; }
        public int Блюдо_id { get; set; } // Добавлено
    }
}