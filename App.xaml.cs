using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Windows;

namespace MenuStolovaya
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Проверяем подключение к базе данных
            if (!CheckDatabaseConnection())
            {
                // Показываем окно настройки
                var setupWindow = new Views.DatabaseSetupWindow();
                var result = setupWindow.ShowDialog();

                if (result != true)
                {
                    // Пользователь закрыл окно настройки - выходим
                    Shutdown();
                    return;
                }

                // Ещё раз проверяем подключение после сохранения настроек
                if (!CheckDatabaseConnection())
                {
                    MessageBox.Show(
                        "Не удалось подключиться к базе данных после сохранения настроек.\n" +
                        "Проверьте параметры подключения и запустите программу снова.",
                        "Ошибка подключения",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    Shutdown();
                    return;
                }
            }

            // Запускаем окно входа
            var loginWindow = new Views.LoginWindow();
            loginWindow.Show();
        }

        /// <summary>
        /// Проверяет подключение к базе данных
        /// </summary>
        private bool CheckDatabaseConnection()
        {
            try
            {
                using (var db = new MenuStolovayaDBEntities())
                {
                    db.Database.Connection.Open();
                    db.Database.Connection.Close();
                    Debug.WriteLine("✓ Подключение к БД успешно");
                    return true;
                }
            }
            catch (SqlException ex)
            {
                Debug.WriteLine($"✗ Ошибка SQL: {ex.Message}");
                return false;
            }
            catch (EntityException ex)  // Добавлен перехват ошибок Entity Framework
            {
                Debug.WriteLine($"✗ Ошибка Entity Framework: {ex.Message}");
                // Если строка подключения неверна, удаляем её, чтобы при следующем запуске открылось окно настройки
                try
                {
                    var config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                    if (config.ConnectionStrings.ConnectionStrings["MenuStolovayaDBEntities"] != null)
                    {
                        config.ConnectionStrings.ConnectionStrings["MenuStolovayaDBEntities"].ConnectionString = "";
                        config.Save(ConfigurationSaveMode.Modified);
                        ConfigurationManager.RefreshSection("connectionStrings");
                        Debug.WriteLine("✓ Неверная строка подключения удалена");
                    }
                }
                catch { }
                return false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"✗ Ошибка: {ex.Message}");
                return false;
            }
        }
    }
}