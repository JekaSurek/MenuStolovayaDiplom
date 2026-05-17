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
    public partial class LoginWindow : Window
    {
        private UserService _userService;

        public LoginWindow()
        {
            InitializeComponent();
            _userService = new UserService();
            LoginTextBox.Focus();
        }

        private void ButtonLogin_Click(object sender, RoutedEventArgs e)
        {
            string login = LoginTextBox.Text;
            string password = PasswordBox.Password;

            if (string.IsNullOrWhiteSpace(login) || string.IsNullOrWhiteSpace(password))
            {
                MessageBox.Show("Введите логин и пароль", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            _userService.AuthenticateUser(login, password, this);
        }

        private void LoginTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateLoginButtonState();
        }

        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            UpdateLoginButtonState();
        }

        private void UpdateLoginButtonState()
        {
            ButtonLogin.IsEnabled = !string.IsNullOrWhiteSpace(LoginTextBox.Text) &&
                                   !string.IsNullOrWhiteSpace(PasswordBox.Password);
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