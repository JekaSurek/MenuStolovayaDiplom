using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MenuStolovaya.Services
{
    public static class HolidayService
    {
        // Фиксированные праздничные даты (день и месяц)
        private static readonly List<(int Month, int Day)> FixedHolidays = new List<(int, int)>
        {
            (1, 1),   // Новый год
            (1, 2),   // Новый год
            (1, 3),   // Новый год
            (1, 4),   // Новый год
            (1, 5),   // Новый год
            (1, 6),   // Новый год
            (1, 7),   // Рождество
            (1, 8),   // Новый год
            (2, 23),  // День защитника Отечества
            (3, 8),   // Международный женский день
            (5, 1),   // Праздник Весны и Труда
            (5, 9),   // День Победы
            (6, 12),  // День России
            (11, 4),  // День народного единства
        };

        // Переносимые праздники (даты могут меняться)
        // Для упрощения используем приблизительные расчёты
        private static DateTime GetEasterSunday(int year)
        {
            // Алгоритм расчета даты Пасхи (алгоритм Гаусса)
            int a = year % 19;
            int b = year % 4;
            int c = year % 7;
            int k = year / 100;
            int p = (13 + 8 * k) / 25;
            int q = k / 4;
            int m = (15 - p + k - q) % 30;
            int n = (4 + k - q) % 7;
            int d = (19 * a + m) % 30;
            int e = (2 * b + 4 * c + 6 * d + n) % 7;
            int day = 22 + d + e;
            int month = 3;
            if (day > 31)
            {
                day -= 31;
                month = 4;
            }
            return new DateTime(year, month, day);
        }

        public static bool IsHoliday(DateTime date)
        {
            // Проверка выходного дня
            if (date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday)
                return true;

            // Проверка фиксированных праздников
            foreach (var holiday in FixedHolidays)
            {
                if (date.Month == holiday.Month && date.Day == holiday.Day)
                    return true;
            }

            // Проверка переносимых праздников (Пасха)
            var easter = GetEasterSunday(date.Year);
            var easterMonday = easter.AddDays(1);

            if (date.Date == easter.Date || date.Date == easterMonday.Date)
                return true;

            return false;
        }

        public static DateTime GetNextWorkDay(DateTime date)
        {
            DateTime nextDay = date.AddDays(1);
            while (IsHoliday(nextDay))
            {
                nextDay = nextDay.AddDays(1);
            }
            return nextDay;
        }

        public static string GetHolidayName(DateTime date)
        {
            // Проверка фиксированных праздников
            foreach (var holiday in FixedHolidays)
            {
                if (date.Month == holiday.Month && date.Day == holiday.Day)
                {
                    return GetFixedHolidayName(holiday.Month, holiday.Day);
                }
            }

            // Пасха
            var easter = GetEasterSunday(date.Year);
            if (date.Date == easter.Date)
                return "Пасха";
            if (date.Date == easter.AddDays(1).Date)
                return "Пасхальный понедельник";

            // Выходной
            if (date.DayOfWeek == DayOfWeek.Saturday)
                return "Суббота";
            if (date.DayOfWeek == DayOfWeek.Sunday)
                return "Воскресенье";

            return null;
        }

        private static string GetFixedHolidayName(int month, int day)
        {
            if (month == 1 && (day >= 1 && day <= 8))
                return "Новогодние каникулы";
            if (month == 2 && day == 23)
                return "День защитника Отечества";
            if (month == 3 && day == 8)
                return "Международный женский день";
            if (month == 5 && day == 1)
                return "Праздник Весны и Труда";
            if (month == 5 && day == 9)
                return "День Победы";
            if (month == 6 && day == 12)
                return "День России";
            if (month == 11 && day == 4)
                return "День народного единства";
            return "Праздничный день";
        }
    }
}