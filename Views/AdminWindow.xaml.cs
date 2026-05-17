using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using MenuStolovaya.Models;
using System.Data.Entity;

namespace MenuStolovaya.Views
{
    public partial class AdminWindow : Window
    {
        private List<Пользователи> _users;
        private Пользователи _selectedUser;

        public AdminWindow()
        {
            InitializeComponent();
            LoadCurrentUserInfo();
            LoadUsers();
        }

        private void LoadCurrentUserInfo()
        {
            CurrentUserText.Text = Models.ThisUser.CurrentUser?.FullName ?? "Неизвестно";
        }

        private void LoadUsers(string filter = "")
        {
            try
            {
                using (var db = new MenuStolovayaDBEntities())
                {
                    // Явно загружаем связанные данные
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

                    // Создаем список объектов UserDisplay
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
                        // Добавляем пароль для отладки (в реальном приложении лучше не показывать)
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

            // Проверка на удаление самого себя
            if (_selectedUser.id == Models.ThisUser.CurrentUser.Id)
            {
                MessageBox.Show("Нельзя удалить самого себя",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Проверка на администратора
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
            Models.ThisUser.ClearCurrentUser();
            var loginWindow = new LoginWindow();
            loginWindow.Show();
            this.Close();
        }
    }
}