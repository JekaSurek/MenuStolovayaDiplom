using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using MenuStolovaya.Models;
using System.Data.Entity;

namespace MenuStolovaya.Views
{
    public partial class AdminWindow : Window
    {
        private List<Пользователи> _users;
        private Пользователи _selectedUser;
        private Dictionary<Button, Style> _buttonStyles = new Dictionary<Button, Style>();
        private bool _isMaximized = false;

        public AdminWindow()
        {
            InitializeComponent();

            // Сохраняем стили кнопок
            _buttonStyles[UsersTabButton] = UsersTabButton.Style;
            _buttonStyles[HelpTabButton] = HelpTabButton.Style;

            LoadCurrentUserInfo();
            LoadUsers();

            this.Loaded += (s, e) => ShowTab(UsersContent, UsersTabButton);
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void MaximizeButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isMaximized)
            {
                this.WindowState = WindowState.Normal;
                _isMaximized = false;
                MaximizeButton.Content = "□";
                MaximizeButton.ToolTip = "Развернуть";
            }
            else
            {
                this.WindowState = WindowState.Maximized;
                _isMaximized = true;
                MaximizeButton.Content = "❐";
                MaximizeButton.ToolTip = "Восстановить";
            }
        }

        // Если окно сворачивается/разворачивается другими способами, добавьте обработчик
        protected override void OnStateChanged(EventArgs e)
        {
            base.OnStateChanged(e);

            if (this.WindowState == WindowState.Maximized)
            {
                _isMaximized = true;
                MaximizeButton.Content = "❐";
                MaximizeButton.ToolTip = "Восстановить";
            }
            else if (this.WindowState == WindowState.Normal)
            {
                _isMaximized = false;
                MaximizeButton.Content = "□";
                MaximizeButton.ToolTip = "Развернуть";
            }
        }

        private void LoadCurrentUserInfo()
        {
            CurrentUserText.Text = ThisUser.CurrentUser?.FullName ?? "Неизвестно";
        }

        private void ShowTab(Grid tabContent, Button activeButton)
        {
            UsersContent.Visibility = Visibility.Collapsed;
            HelpContent.Visibility = Visibility.Collapsed;

            tabContent.Visibility = Visibility.Visible;

            // Сбрасываем стили всех кнопок
            foreach (var btn in _buttonStyles.Keys)
            {
                btn.Style = _buttonStyles[btn];
            }

            // Устанавливаем активный стиль
            activeButton.Style = (Style)FindResource("ActiveTabButtonStyle");
        }

        private void LoadUsers(string filter = "")
        {
            try
            {
                using (var db = new MenuStolovayaDBEntities())
                {
                    var query = db.Пользователи
                        .Include(u => u.Роли)
                        .AsQueryable();

                    if (!string.IsNullOrWhiteSpace(filter))
                    {
                        query = query.Where(u =>
                            u.Фамилия.Contains(filter) ||
                            u.Имя.Contains(filter) ||
                            (u.Отчество != null && u.Отчество.Contains(filter)) ||
                            u.Логин.Contains(filter) ||
                            (u.Роли != null && u.Роли.Наименование.Contains(filter)));
                    }

                    _users = query.ToList();

                    var usersForDisplay = _users.Select(u => new UserDisplay
                    {
                        Id = u.id,
                        Логин = u.Логин,
                        Фамилия = u.Фамилия,
                        Имя = u.Имя,
                        Отчество = u.Отчество ?? "",
                        ФИО = $"{u.Фамилия} {u.Имя} {u.Отчество}".Trim(),
                        Роль = u.Роли?.Наименование ?? "Не назначена",
                        Блокировка = u.Блокировка ?? false,
                        Дата_регистрации = u.Дата_регистрации.HasValue
                            ? u.Дата_регистрации.Value.ToString("dd.MM.yyyy")
                            : "Не указана",
                        Пароль = u.Пароль
                    }).ToList();

                    UsersDataGrid.ItemsSource = usersForDisplay;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке пользователей: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UsersTabButton_Click(object sender, RoutedEventArgs e)
        {
            ShowTab(UsersContent, UsersTabButton);
            LoadUsers(SearchTextBox.Text);
        }

        private void HelpTabButton_Click(object sender, RoutedEventArgs e)
        {
            ShowTab(HelpContent, HelpTabButton);
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            LoadUsers(SearchTextBox.Text);
        }

        private void ClearSearchButton_Click(object sender, RoutedEventArgs e)
        {
            SearchTextBox.Text = "";
            LoadUsers();
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            LoadUsers(SearchTextBox.Text);
        }

        private void UsersDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (UsersDataGrid.SelectedItem is UserDisplay selectedDisplay)
            {
                _selectedUser = _users.FirstOrDefault(u => u.id == selectedDisplay.Id);
            }
            else
            {
                _selectedUser = null;
            }

            bool hasSelection = _selectedUser != null;
            EditUserButton.IsEnabled = hasSelection;
            DeleteUserButton.IsEnabled = hasSelection;
        }

        private void AddUserButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var addUserWindow = new AddEditUserWindow(null);
                if (addUserWindow.ShowDialog() == true)
                {
                    LoadUsers(SearchTextBox.Text);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при добавлении пользователя: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void EditUserButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedUser == null)
            {
                MessageBox.Show("Выберите пользователя для редактирования",
                    "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var editUserWindow = new AddEditUserWindow(_selectedUser);
                if (editUserWindow.ShowDialog() == true)
                {
                    LoadUsers(SearchTextBox.Text);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при редактировании пользователя: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DeleteUserButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedUser == null)
            {
                MessageBox.Show("Выберите пользователя для удаления",
                    "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (_selectedUser.id == ThisUser.CurrentUser.Id)
            {
                MessageBox.Show("Нельзя удалить самого себя",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (_selectedUser.Роли?.Наименование == "Администратор")
            {
                MessageBox.Show("Нельзя удалить администратора",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var result = MessageBox.Show($"Вы уверены, что хотите удалить пользователя {_selectedUser.Фамилия} {_selectedUser.Имя}?",
                "Подтверждение удаления", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    using (var db = new MenuStolovayaDBEntities())
                    {
                        var userToDelete = db.Пользователи.Find(_selectedUser.id);
                        if (userToDelete != null)
                        {
                            db.Пользователи.Remove(userToDelete);
                            db.SaveChanges();
                            LoadUsers(SearchTextBox.Text);
                            MessageBox.Show("Пользователь успешно удален",
                                "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при удалении пользователя: {ex.Message}",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            ThisUser.ClearCurrentUser();
            var loginWindow = new LoginWindow();
            loginWindow.Show();
            this.Close();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Вы уверены, что хотите выйти из программы?",
                "Подтверждение выхода",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                Application.Current.Shutdown();
            }
        }

        private void HelpButton_Click(object sender, RoutedEventArgs e)
        {
            string helpPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Help", "help.html");

            if (System.IO.File.Exists(helpPath))
            {
                System.Diagnostics.Process.Start(helpPath);
            }
            else
            {
                MessageBox.Show("Файл справки не найден!\n\n" +
                               "Ожидаемый путь: " + helpPath,
                               "Ошибка",
                               MessageBoxButton.OK,
                               MessageBoxImage.Error);
            }
        }
    }
}