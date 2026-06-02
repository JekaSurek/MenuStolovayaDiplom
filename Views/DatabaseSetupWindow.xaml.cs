using System;
using System.Configuration;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Xml;

namespace MenuStolovaya.Views
{
    public partial class DatabaseSetupWindow : Window
    {
        private static string SettingsFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "MenuStolovaya", "connection.config");

        public DatabaseSetupWindow()
        {
            InitializeComponent();
            LoadCurrentSettings();
        }

        private void LoadCurrentSettings()
        {
            try
            {
                // Пробуем загрузить из пользовательского файла
                if (File.Exists(SettingsFilePath))
                {
                    string savedConnectionString = File.ReadAllText(SettingsFilePath);
                    if (!string.IsNullOrEmpty(savedConnectionString))
                    {
                        var dataSource = ExtractValue(savedConnectionString, "data source");
                        var initialCatalog = ExtractValue(savedConnectionString, "initial catalog");

                        if (!string.IsNullOrEmpty(dataSource))
                            ServerTextBox.Text = dataSource;
                        if (!string.IsNullOrEmpty(initialCatalog))
                            DatabaseTextBox.Text = initialCatalog;
                        return;
                    }
                }

                // Если нет, пробуем из App.config
                var config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                var connectionString = config.ConnectionStrings.ConnectionStrings["MenuStolovayaDBEntities"]?.ConnectionString;

                if (!string.IsNullOrEmpty(connectionString))
                {
                    var dataSource = ExtractValue(connectionString, "data source");
                    var initialCatalog = ExtractValue(connectionString, "initial catalog");

                    if (!string.IsNullOrEmpty(dataSource))
                        ServerTextBox.Text = dataSource;
                    if (!string.IsNullOrEmpty(initialCatalog))
                        DatabaseTextBox.Text = initialCatalog;
                }
                else
                {
                    ServerTextBox.Text = ".";
                    DatabaseTextBox.Text = "MenuStolovayaDB";
                }
            }
            catch (Exception ex)
            {
                StatusText.Text = $"Ошибка загрузки: {ex.Message}";
                // Значения по умолчанию
                ServerTextBox.Text = ".";
                DatabaseTextBox.Text = "MenuStolovayaDB";
            }
        }

        private string ExtractValue(string connectionString, string key)
        {
            var pattern = $"{key}=([^;]+)";
            var match = System.Text.RegularExpressions.Regex.Match(connectionString, pattern,
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            return match.Success ? match.Groups[1].Value : "";
        }

        private void BrowseServerButton_Click(object sender, RoutedEventArgs e)
        {
            StatusText.Text = "Примеры:\n• . или localhost - локальный SQL Server\n• .\\SQLEXPRESS - SQL Server Express\n• ИМЯ_КОМПЬЮТЕРА\\ИМЯ_ЭКЗЕМПЛЯРА";
            StatusText.Foreground = System.Windows.Media.Brushes.Blue;
        }

        private void AuthType_Changed(object sender, RoutedEventArgs e)
        {
            if (SqlAuthRadio == null || SqlAuthPanel == null)
                return;

            SqlAuthPanel.Visibility = SqlAuthRadio.IsChecked == true
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        private void TestConnectionButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(ServerTextBox.Text))
            {
                StatusText.Text = "✗ Укажите сервер";
                StatusText.Foreground = System.Windows.Media.Brushes.Red;
                return;
            }

            if (string.IsNullOrEmpty(DatabaseTextBox.Text))
            {
                StatusText.Text = "✗ Укажите имя базы данных";
                StatusText.Foreground = System.Windows.Media.Brushes.Red;
                return;
            }

            string connectionString = BuildSqlConnectionString();

            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    connection.Close();

                    StatusText.Text = "✓ Подключение успешно!";
                    StatusText.Foreground = System.Windows.Media.Brushes.Green;
                }
            }
            catch (Exception ex)
            {
                StatusText.Text = $"✗ Ошибка: {ex.Message}";
                StatusText.Foreground = System.Windows.Media.Brushes.Red;
            }
        }

        private string BuildSqlConnectionString()
        {
            string server = ServerTextBox.Text.Trim();
            string database = DatabaseTextBox.Text.Trim();

            if (WindowsAuthRadio != null && WindowsAuthRadio.IsChecked == true)
            {
                return $"data source={server};initial catalog={database};integrated security=True;TrustServerCertificate=True";
            }
            else
            {
                string login = LoginTextBox?.Text.Trim() ?? "";
                string password = PasswordBox?.Password ?? "";
                return $"data source={server};initial catalog={database};user id={login};password={password};TrustServerCertificate=True";
            }
        }

        private string BuildEntityConnectionString(string sqlConnectionString)
        {
            string escapedSql = sqlConnectionString.Replace("\"", "&quot;");

            return $"metadata=res://*/ModelMenuStolovayaDB.csdl|res://*/ModelMenuStolovayaDB.ssdl|res://*/ModelMenuStolovayaDB.msl;" +
                   $"provider=System.Data.SqlClient;" +
                   $"provider connection string=\"{escapedSql};MultipleActiveResultSets=True;App=EntityFramework\"";
        }

        private void SaveConnectionString(string connectionString)
        {
            // Сохраняем в пользовательский файл
            string directory = Path.GetDirectoryName(SettingsFilePath);
            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            File.WriteAllText(SettingsFilePath, connectionString);

            // Также пробуем сохранить в App.config (если есть права)
            try
            {
                var config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);

                if (config.ConnectionStrings.ConnectionStrings["MenuStolovayaDBEntities"] != null)
                {
                    config.ConnectionStrings.ConnectionStrings["MenuStolovayaDBEntities"].ConnectionString = connectionString;
                }
                else
                {
                    config.ConnectionStrings.ConnectionStrings.Add(
                        new ConnectionStringSettings("MenuStolovayaDBEntities", connectionString, "System.Data.EntityClient"));
                }

                config.Save(ConfigurationSaveMode.Modified);
                ConfigurationManager.RefreshSection("connectionStrings");
            }
            catch (UnauthorizedAccessException)
            {
                // Нет прав на запись в App.config — игнорируем, пользовательский файл уже сохранён
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(ServerTextBox.Text))
            {
                MessageBox.Show("Укажите сервер базы данных", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrEmpty(DatabaseTextBox.Text))
            {
                MessageBox.Show("Укажите имя базы данных", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (SqlAuthRadio != null && SqlAuthRadio.IsChecked == true)
            {
                if (string.IsNullOrEmpty(LoginTextBox?.Text))
                {
                    MessageBox.Show("Укажите логин", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }

            string sqlConnectionString = BuildSqlConnectionString();

            try
            {
                using (var connection = new SqlConnection(sqlConnectionString))
                {
                    connection.Open();
                    connection.Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Не удалось подключиться:\n{ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            string entityConnectionString = BuildEntityConnectionString(sqlConnectionString);

            try
            {
                SaveConnectionString(entityConnectionString);

                MessageBox.Show("Настройки сохранены!", "Успех",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                // Устанавливаем DialogResult только если окно было открыто как диалог
                if (IsVisible && Owner != null)
                {
                    DialogResult = true;
                }
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            if (IsVisible && Owner != null)
            {
                DialogResult = false;
            }
            Close();
        }

        private void HelpButton_Click(object sender, RoutedEventArgs e)
        {
            string helpPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Help", "help.html");

            if (System.IO.File.Exists(helpPath))
            {
                Process.Start(new ProcessStartInfo(helpPath) { UseShellExecute = true });
            }
            else
            {
                MessageBox.Show("Файл справки не найден!\n\nОжидаемый путь: " + helpPath,
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}