using System;
using System.Linq;
using System.Windows;
using MenuStolovaya.Models;
using MenuStolovaya.Services;

namespace MenuStolovaya.Views
{
    public partial class TechCardDetailsDialog : Window
    {
        private int _techCardId;
        private AccountantTechCardService _service;

        public TechCardDetailsDialog(int techCardId)
        {
            InitializeComponent();
            _techCardId = techCardId;
            _service = new AccountantTechCardService();
            LoadTechCardDetails();
        }

        private void LoadTechCardDetails()
        {
            try
            {
                var details = _service.GetTechCardDetails(_techCardId);
                if (details == null) return;

                // Заголовок
                TitleText.Text = $"Технологическая карта - {details.Номер}";

                // Основная информация
                CardNumberText.Text = details.Номер;
                DishText.Text = details.Блюдо;
                DishTypeText.Text = details.Вид_блюда;
                OutputText.Text = $"{details.Выход:N1} г";
                StatusText.Text = details.Статус;
                CreationDateText.Text = details.Дата_создания.ToString("dd.MM.yyyy HH:mm");

                // Технология приготовления
                TechnologyText.Text = details.Технология_приготовления ?? "Не указана";

                // Цвет статуса
                if (details.Статус == "Утверждена")
                {
                    StatusText.Foreground = System.Windows.Media.Brushes.Green;
                }
                else if (details.Статус == "Черновик")
                {
                    StatusText.Foreground = System.Windows.Media.Brushes.Orange;
                }
                else
                {
                    StatusText.Foreground = System.Windows.Media.Brushes.Red;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке деталей технологической карты: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void PrintButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var printDialog = new System.Windows.Controls.PrintDialog();
                if (printDialog.ShowDialog() == true)
                {
                    printDialog.PrintVisual(this, $"Технологическая карта {_techCardId}");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при печати: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}