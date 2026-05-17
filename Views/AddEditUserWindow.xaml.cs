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
using System.Windows.Shapes;
using MenuStolovaya.Models;

namespace MenuStolovaya.Views
{
    public partial class AddEditUserWindow : Window
    {
        public UserModel User { get; private set; }
        public string WindowTitle { get; private set; }

        public AddEditUserWindow(Пользователи user = null)
        {
            InitializeComponent();
            DataContext = this;

            if (user == null)
            {
                // Добавление нового пользователя
                User = new UserModel
                {
                    Блокировка = false,
                    Дата_регистрации = DateTime.Now
                };
                WindowTitle = "Добавление пользователя";
            }
            else
            {
                // Редактирование существующего пользователя
                User = new UserModel
                {
                    Id = user.id,
                    Логин = user.Логин,
                    Пароль = user.Пароль,
                    Фамилия = user.Фамилия,
                    Имя = user.Имя,
                    Отчество = user.Отчество ?? "",
                    Роль_id = user.Роль_id,
                    Блокировка = user.Блокировка ?? false, // Исправление для nullable
                    Дата_регистрации = user.Дата_регистрации ?? DateTime.Now // Исправление для nullable DateTime
                };
                WindowTitle = "Редактирование пользователя";
            }

            LoadRoles();
        }

        private void LoadRoles()
        {
            try
            {
                using (var db = new MenuStolovayaDBEntities())
                {
                    var roles = db.Роли.ToList();
                    RoleComboBox.ItemsSource = roles;

                    if (User.Id > 0) // Редактирование
                    {
                        RoleComboBox.SelectedValue = User.Роль_id;
                    }
                    else // Добавление
                    {
                        // По умолчанию выбираем роль "Технолог"
                        var defaultRole = roles.FirstOrDefault(r => r.Наименование == "Технолог");
                        if (defaultRole != null)
                        {
                            User.Роль_id = defaultRole.id;
                            RoleComboBox.SelectedValue = defaultRole.id;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке ролей: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            // Валидация
            if (string.IsNullOrWhiteSpace(User.Логин))
            {
                MessageBox.Show("Введите логин", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                LoginTextBox.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(User.Пароль))
            {
                MessageBox.Show("Введите пароль", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                PasswordTextBox.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(User.Фамилия) || string.IsNullOrWhiteSpace(User.Имя))
            {
                MessageBox.Show("Введите фамилию и имя", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (User.Роль_id <= 0)
            {
                MessageBox.Show("Выберите роль", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                RoleComboBox.Focus();
                return;
            }



            try
            {
                using (var db = new MenuStolovayaDBEntities())
                {
                    if (User.Id == 0)
                    {
                        // Проверка на уникальность логина
                        if (db.Пользователи.Any(u => u.Логин == User.Логин))
                        {
                            MessageBox.Show("Пользователь с таким логином уже существует",
                                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }

                        // Добавление нового пользователя (Entity Framework класс)
                        var newUser = new Пользователи
                        {
                            Логин = User.Логин,
                            Пароль = User.Пароль,
                            Фамилия = User.Фамилия,
                            Имя = User.Имя,
                            Отчество = User.Отчество,
                            Роль_id = User.Роль_id,
                            Блокировка = User.Блокировка,
                            Дата_регистрации = DateTime.Now
                        };

                        db.Пользователи.Add(newUser);
                        db.SaveChanges();
                    }
                    else
                    {
                        // Редактирование существующего пользователя
                        var existingUser = db.Пользователи.Find(User.Id);
                        if (existingUser != null)
                        {
                            // Проверка на уникальность логина (если изменили)
                            if (existingUser.Логин != User.Логин &&
                                db.Пользователи.Any(u => u.Логин == User.Логин && u.id != User.Id))
                            {
                                MessageBox.Show("Пользователь с таким логином уже существует",
                                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                                return;
                            }

                            existingUser.Логин = User.Логин;
                            existingUser.Пароль = User.Пароль;
                            existingUser.Фамилия = User.Фамилия;
                            existingUser.Имя = User.Имя;
                            existingUser.Отчество = User.Отчество;
                            existingUser.Роль_id = User.Роль_id;
                            existingUser.Блокировка = User.Блокировка;
                           

                            db.SaveChanges();
                        }
                    }
                }

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении пользователя: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            // Можно добавить логику валидации пароля в реальном времени
        }

        private void ConfirmPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            // Можно добавить логику проверки совпадения паролей в реальном времени
        }
    }
}