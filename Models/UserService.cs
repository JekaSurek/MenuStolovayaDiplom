using MenuStolovaya.Views;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace MenuStolovaya.Models
{
    public class UserService
    {
        public bool AuthenticateUser(string login, string password, Window window)
        {
            try
            {
                using (var db = new MenuStolovayaDBEntities())
                {
                    var user = db.Пользователи
                        .Include(u => u.Роли)
                        .FirstOrDefault(u => u.Логин == login && u.Пароль == password);

                    if (user != null)
                    {
                        // Исправление для nullable bool
                        if (user.Блокировка ?? false)
                        {
                            MessageBox.Show("Пользователь заблокирован. Обратитесь к администратору.",
                                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                            return false;
                        }

                        string roleName = user.Роли?.Наименование ?? "Неизвестная роль";
                        ThisUser.SetCurrentUser(user, roleName);

                        // Открываем соответствующее окно в зависимости от роли
                        if (roleName == "Администратор")
                        {
                            var adminWindow = new Views.AdminWindow();
                            adminWindow.Show();
                            window.Close();
                        }
                        else if (roleName == "Технолог")
                        {
                            var technologistWindow = new Views.TechnologistWindow();
                            technologistWindow.Show();
                            window.Close();
                        }
                        else if (roleName == "Бухгалтер")
                        {
                            var accountantWindow = new Views.AccountantWindow();
                            accountantWindow.Show();
                            window.Close();
                        }
                        else
                        {
                            MessageBox.Show("Неизвестная роль пользователя", "Ошибка",
                                MessageBoxButton.OK, MessageBoxImage.Error);
                            return false;
                        }

                        return true;
                    }
                    else
                    {
                        MessageBox.Show("Неверный логин или пароль", "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при авторизации: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }
    }
}