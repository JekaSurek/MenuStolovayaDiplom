using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;
using System.IO;

namespace MenuStolovaya.Models
{
    public static class DatabaseHelper
    {
        private static string SettingsFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "MenuStolovaya", "connection.config");

        public static string GetConnectionString()
        {
            // Сначала пробуем загрузить из пользовательского файла
            if (File.Exists(SettingsFilePath))
            {
                return File.ReadAllText(SettingsFilePath);
            }

            // Если нет, берём из App.config
            return ConfigurationManager.ConnectionStrings["MenuStolovayaDBEntities"]?.ConnectionString;
        }
    }
}