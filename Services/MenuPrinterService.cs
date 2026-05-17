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

                    // Сначала получаем данные из БД (без вызова методов в LINQ)
                    var menuItemsRaw = db.Строки_меню
                        .Where(sm => sm.Меню_id == menuId)
                        .Join(db.Блюда,
                            sm => sm.Блюдо_id,
                            b => b.id,
                            (sm, b) => new
                            {
                                Id = sm.id,
                                Блюдо_id = b.id,
                                Блюдо = b.Наименование,
                                Полное_наименование = b.Полное_наименование,
                                Вид_блюда = b.Виды_блюд != null ? b.Виды_блюд.Наименование : "Не указано",
                                Количество_порций = sm.Количество_порций ?? 1,
                                Выход_на_порцию = sm.Выход_на_порцию ?? b.Выход_стандартный ?? 100,
                                Время_подачи = sm.Время_подачи ?? "Обед",
                                Порядок_подачи = sm.Порядок_подачи ?? 0,
                                Калорийность_расчетная = b.Калорийность_расчетная ?? 0
                            })
                        .ToList();

                    // Преобразуем в MenuItemWithDetails
                    var menuItems = new List<MenuItemForMenu>();
                    foreach (var item in menuItemsRaw)
                    {
                        menuItems.Add(new MenuItemForMenu
                        {
                            Id = item.Id,
                            Блюдо = item.Блюдо,
                            Полное_наименование = item.Полное_наименование,
                            Вид_блюда = item.Вид_блюда,
                            Выход_на_порцию = item.Выход_на_порцию,
                            Время_подачи = item.Время_подачи,
                            Порядок_подачи = item.Порядок_подачи,
                            Калорийность_расчетная = item.Калорийность_расчетная,
                            Блюдо_id = item.Блюдо_id,
                            Цена = GetDishPrice(item.Блюдо_id)
                        });
                    }

                    // Группируем по времени подачи
                    var timeGroups = menuItems
                        .GroupBy(m => m.Время_подачи)
                        .OrderBy(g => GetTimeOrder(g.Key));

                    StringBuilder html = new StringBuilder();
                    html.AppendLine("<!DOCTYPE html>");
                    html.AppendLine("<html lang='ru'>");
                    html.AppendLine("<head>");
                    html.AppendLine("    <meta charset='UTF-8'>");
                    html.AppendLine("    <meta name='viewport' content='width=device-width, initial-scale=1.0'>");
                    html.AppendLine("    <title>Меню столовой</title>");
                    html.AppendLine("    <style>");
                    html.AppendLine("        * { margin: 0; padding: 0; box-sizing: border-box; }");
                    html.AppendLine("        body {");
                    html.AppendLine("            font-family: 'Segoe UI', 'Arial', sans-serif;");
                    html.AppendLine("            background: #f5f5f5;");
                    html.AppendLine("            margin: 0;");
                    html.AppendLine("            padding: 20px;");
                    html.AppendLine("        }");
                    html.AppendLine("        .menu-container {");
                    html.AppendLine("            max-width: 1000px;");
                    html.AppendLine("            margin: 0 auto;");
                    html.AppendLine("            background: white;");
                    html.AppendLine("            border-radius: 12px;");
                    html.AppendLine("            box-shadow: 0 10px 40px rgba(0,0,0,0.1);");
                    html.AppendLine("            overflow: hidden;");
                    html.AppendLine("        }");
                    html.AppendLine("        .menu-header {");
                    html.AppendLine("            background: linear-gradient(135deg, #2e7d32 0%, #1b5e20 100%);");
                    html.AppendLine("            color: white;");
                    html.AppendLine("            padding: 30px;");
                    html.AppendLine("            text-align: center;");
                    html.AppendLine("        }");
                    html.AppendLine("        .menu-header h1 {");
                    html.AppendLine("            font-size: 32px;");
                    html.AppendLine("            margin-bottom: 10px;");
                    html.AppendLine("            letter-spacing: 2px;");
                    html.AppendLine("        }");
                    html.AppendLine("        .menu-header .date {");
                    html.AppendLine("            font-size: 18px;");
                    html.AppendLine("            opacity: 0.9;");
                    html.AppendLine("            margin-bottom: 5px;");
                    html.AppendLine("        }");
                    html.AppendLine("        .menu-header .responsible {");
                    html.AppendLine("            font-size: 14px;");
                    html.AppendLine("            opacity: 0.8;");
                    html.AppendLine("            margin-top: 15px;");
                    html.AppendLine("        }");
                    html.AppendLine("        .menu-body {");
                    html.AppendLine("            padding: 30px;");
                    html.AppendLine("        }");
                    html.AppendLine("        .time-section {");
                    html.AppendLine("            margin-bottom: 40px;");
                    html.AppendLine("        }");
                    html.AppendLine("        .time-title {");
                    html.AppendLine("            font-size: 24px;");
                    html.AppendLine("            font-weight: bold;");
                    html.AppendLine("            color: #2e7d32;");
                    html.AppendLine("            border-left: 5px solid #2e7d32;");
                    html.AppendLine("            padding-left: 15px;");
                    html.AppendLine("            margin-bottom: 20px;");
                    html.AppendLine("        }");
                    html.AppendLine("        .dish-type-group {");
                    html.AppendLine("            margin-bottom: 25px;");
                    html.AppendLine("        }");
                    html.AppendLine("        .dish-type-title {");
                    html.AppendLine("            font-size: 18px;");
                    html.AppendLine("            font-weight: 600;");
                    html.AppendLine("            color: #666;");
                    html.AppendLine("            background: #f0f0f0;");
                    html.AppendLine("            padding: 8px 15px;");
                    html.AppendLine("            border-radius: 8px;");
                    html.AppendLine("            margin-bottom: 15px;");
                    html.AppendLine("        }");
                    html.AppendLine("        .menu-table {");
                    html.AppendLine("            width: 100%;");
                    html.AppendLine("            border-collapse: collapse;");
                    html.AppendLine("        }");
                    html.AppendLine("        .menu-table th {");
                    html.AppendLine("            text-align: left;");
                    html.AppendLine("            padding: 12px;");
                    html.AppendLine("            background: #f8f9fa;");
                    html.AppendLine("            border-bottom: 2px solid #dee2e6;");
                    html.AppendLine("            font-weight: 600;");
                    html.AppendLine("            color: #495057;");
                    html.AppendLine("        }");
                    html.AppendLine("        .menu-table td {");
                    html.AppendLine("            padding: 15px 12px;");
                    html.AppendLine("            border-bottom: 1px solid #e9ecef;");
                    html.AppendLine("            vertical-align: top;");
                    html.AppendLine("        }");
                    html.AppendLine("        .menu-table tr:last-child td {");
                    html.AppendLine("            border-bottom: none;");
                    html.AppendLine("        }");
                    html.AppendLine("        .dish-name {");
                    html.AppendLine("            font-weight: 600;");
                    html.AppendLine("            font-size: 16px;");
                    html.AppendLine("            color: #2c3e50;");
                    html.AppendLine("        }");
                    html.AppendLine("        .dish-description {");
                    html.AppendLine("            font-size: 13px;");
                    html.AppendLine("            color: #6c757d;");
                    html.AppendLine("            margin-top: 4px;");
                    html.AppendLine("        }");
                    html.AppendLine("        .portion-weight {");
                    html.AppendLine("            font-weight: 600;");
                    html.AppendLine("            color: #2e7d32;");
                    html.AppendLine("            font-size: 14px;");
                    html.AppendLine("        }");
                    html.AppendLine("        .nutrients {");
                    html.AppendLine("            font-size: 13px;");
                    html.AppendLine("            color: #6c757d;");
                    html.AppendLine("        }");
                    html.AppendLine("        .calories {");
                    html.AppendLine("            font-weight: 600;");
                    html.AppendLine("            color: #e67e22;");
                    html.AppendLine("        }");
                    html.AppendLine("        .price {");
                    html.AppendLine("            font-weight: 700;");
                    html.AppendLine("            font-size: 18px;");
                    html.AppendLine("            color: #e74c3c;");
                    html.AppendLine("            white-space: nowrap;");
                    html.AppendLine("        }");
                    html.AppendLine("        .allergy-info {");
                    html.AppendLine("            background: #fff3cd;");
                    html.AppendLine("            border-left: 4px solid #ffc107;");
                    html.AppendLine("            padding: 12px 20px;");
                    html.AppendLine("            margin: 20px 0;");
                    html.AppendLine("            border-radius: 8px;");
                    html.AppendLine("            font-size: 13px;");
                    html.AppendLine("        }");
                    html.AppendLine("        .signatures {");
                    html.AppendLine("            display: flex;");
                    html.AppendLine("            justify-content: space-between;");
                    html.AppendLine("            margin-top: 40px;");
                    html.AppendLine("            padding-top: 20px;");
                    html.AppendLine("            border-top: 1px solid #dee2e6;");
                    html.AppendLine("        }");
                    html.AppendLine("        .signature-item {");
                    html.AppendLine("            text-align: center;");
                    html.AppendLine("            width: 30%;");
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
                    html.AppendLine("        .footer {");
                    html.AppendLine("            background: #f8f9fa;");
                    html.AppendLine("            padding: 20px 30px;");
                    html.AppendLine("            text-align: center;");
                    html.AppendLine("            border-top: 1px solid #e9ecef;");
                    html.AppendLine("            font-size: 12px;");
                    html.AppendLine("            color: #6c757d;");
                    html.AppendLine("        }");
                    html.AppendLine("        @media print {");
                    html.AppendLine("            body { background: white; padding: 0; }");
                    html.AppendLine("            .menu-container { box-shadow: none; }");
                    html.AppendLine("            .no-print { display: none; }");
                    html.AppendLine("            .menu-header { background: #2e7d32; print-color-adjust: exact; }");
                    html.AppendLine("        }");
                    html.AppendLine("    </style>");
                    html.AppendLine("</head>");
                    html.AppendLine("<body>");
                    html.AppendLine("<div class='menu-container'>");

                    // Шапка меню
                    html.AppendLine("<div class='menu-header'>");
                    html.AppendLine($"    <h1>🍽️ {GetEnterpriseName()}</h1>");
                    html.AppendLine($"    <div class='date'>Меню на {menu.Дата:dd.MM.yyyy} ({GetDayOfWeek(menu.Дата)})</div>");
                    html.AppendLine($"    <div class='responsible'>Ответственный: {menu.Пользователи?.Фамилия} {menu.Пользователи?.Имя}</div>");
                    html.AppendLine("</div>");

                    html.AppendLine("<div class='menu-body'>");

                    // Группировка по времени подачи
                    foreach (var timeGroup in timeGroups)
                    {
                        var dishTypeGroups = timeGroup.GroupBy(m => m.Вид_блюда).OrderBy(g => g.Key);

                        html.AppendLine($"<div class='time-section'>");
                        html.AppendLine($"    <div class='time-title'>⏰ {timeGroup.Key}</div>");

                        foreach (var dishTypeGroup in dishTypeGroups)
                        {
                            html.AppendLine($"    <div class='dish-type-group'>");
                            html.AppendLine($"        <div class='dish-type-title'>🍲 {dishTypeGroup.Key}</div>");
                            html.AppendLine($"        <table class='menu-table'>");
                            html.AppendLine($"            <thead>");
                            html.AppendLine($"                <tr>");
                            html.AppendLine($"                    <th style='width: 45%'>Блюдо</th>");
                            html.AppendLine($"                    <th style='width: 15%'>Вес порции</th>");
                            html.AppendLine($"                    <th style='width: 25%'>Пищевая ценность (на 100г)</th>");
                            html.AppendLine($"                    <th style='width: 15%'>Цена</th>");
                            html.AppendLine($"                </tr>");
                            html.AppendLine($"            </thead>");
                            html.AppendLine($"            <tbody>");

                            foreach (var dish in dishTypeGroup.OrderBy(d => d.Порядок_подачи))
                            {
                                var nutrients = NutrientCalculator.CalculateDishNutrients(dish.Блюдо_id);
                                // Калорийность на 1 порцию (не умножаем на количество порций!)
                                decimal caloriesPerPortion = (dish.Калорийность_расчетная * dish.Выход_на_порцию) / 100;

                                html.AppendLine($"                <tr>");
                                html.AppendLine($"                    <td>");
                                html.AppendLine($"                        <div class='dish-name'>{dish.Блюдо}</div>");
                                if (!string.IsNullOrEmpty(dish.Полное_наименование) && dish.Полное_наименование != dish.Блюдо)
                                {
                                    html.AppendLine($"                        <div class='dish-description'>{dish.Полное_наименование}</div>");
                                }
                                html.AppendLine($"                    </td>");
                                html.AppendLine($"                    <td class='portion-weight'>{dish.Выход_на_порцию:F0} г</td>");
                                html.AppendLine($"                    <td class='nutrients'>");
                                html.AppendLine($"                        <div class='calories'>🔥 {caloriesPerPortion:F0} ккал (в порции)</div>");
                                html.AppendLine($"                        <div>📊 {dish.Калорийность_расчетная:F1} ккал/100г</div>");
                                if (nutrients.Белки > 0 || nutrients.Жиры > 0 || nutrients.Углеводы > 0)
                                {
                                    html.AppendLine($"                        <div>🥩 Б: {nutrients.Белки:F1}г | Ж: {nutrients.Жиры:F1}г | У: {nutrients.Углеводы:F1}г</div>");
                                }
                                html.AppendLine($"                     </td>");
                                html.AppendLine($"                    <td class='price'>{dish.Цена:F2} ₽</td>");
                                html.AppendLine($"                </tr>");
                            }

                            html.AppendLine($"            </tbody>");
                            html.AppendLine($"        </table>");
                            html.AppendLine($"    </div>");
                        }

                        html.AppendLine($"</div>");
                    }

                    // Информация об аллергенах
                    html.AppendLine("<div class='allergy-info'>");
                    html.AppendLine("    ⚠️ <strong>Уважаемые гости!</strong> Обратите внимание: при приготовлении блюд могут использоваться аллергены ");
                    html.AppendLine("    (молоко, яйца, глютен, орехи, соя, рыба, морепродукты, сельдерей, горчица).");
                    html.AppendLine("    <br>При наличии аллергических реакций, пожалуйста, проконсультируйтесь с персоналом столовой.");
                    html.AppendLine("</div>");

                    // Подписи
                    html.AppendLine("<div class='signatures'>");
                    html.AppendLine("    <div class='signature-item'>");
                    html.AppendLine("        <div class='signature-line'></div>");
                    html.AppendLine("        <div class='signature-name'>Директор</div>");
                    html.AppendLine("        <div class='signature-name'>_______________ /Ф.И.О./</div>");
                    html.AppendLine("    </div>");
                    html.AppendLine("    <div class='signature-item'>");
                    html.AppendLine("        <div class='signature-line'></div>");
                    html.AppendLine("        <div class='signature-name'>Шеф-повар</div>");
                    html.AppendLine("        <div class='signature-name'>_______________ /Ф.И.О./</div>");
                    html.AppendLine("    </div>");
                    html.AppendLine("    <div class='signature-item'>");
                    html.AppendLine("        <div class='signature-line'></div>");
                    html.AppendLine("        <div class='signature-name'>Технолог</div>");
                    html.AppendLine("        <div class='signature-name'>_______________ /Ф.И.О./</div>");
                    html.AppendLine("    </div>");
                    html.AppendLine("</div>");

                    html.AppendLine("</div>"); // menu-body

                    // Подвал
                    html.AppendLine("<div class='footer'>");
                    html.AppendLine($"    <div>© {DateTime.Now.Year} {GetEnterpriseName()}. Все права защищены.</div>");
                    html.AppendLine($"    <div style='margin-top: 10px;'>Меню сформировано: {menu.Дата_составления:dd.MM.yyyy HH:mm}</div>");
                    html.AppendLine("    <button class='no-print' onclick='window.print()' style='margin-top: 15px; padding: 8px 20px; background: #2e7d32; color: white; border: none; border-radius: 5px; cursor: pointer;'>🖨️ Распечатать меню</button>");
                    html.AppendLine("</div>");

                    html.AppendLine("</div>"); // menu-container
                    html.AppendLine("</body>");
                    html.AppendLine("</html>");

                    return html.ToString();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при генерации HTML меню: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return $"<html><body><h1>Ошибка при создании меню</h1><p>{ex.Message}</p></body></html>";
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

        private string GetEnterpriseName()
        {
            return "Столовая №1";
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

                // Открываем в браузере в отдельном окне
                var psi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = tempFile,
                    UseShellExecute = true
                };

                System.Diagnostics.Process.Start(psi);

                // Альтернатива - открыть в Internet Explorer без адресной строки
                // System.Diagnostics.Process.Start("iexplore.exe", "-k " + tempFile);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при открытии меню: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    // Класс для меню (без умножения на порции)
    public class MenuItemForMenu
    {
        public int Id { get; set; }
        public string Блюдо { get; set; }
        public string Полное_наименование { get; set; }
        public string Вид_блюда { get; set; }
        public decimal Выход_на_порцию { get; set; }
        public string Время_подачи { get; set; }
        public int Порядок_подачи { get; set; }
        public decimal Калорийность_расчетная { get; set; }
        public decimal Цена { get; set; }
        public int Блюдо_id { get; set; }
    }
}