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
    public partial class RequestDetailsDialog : Window
    {
        private int _requestId;

        public RequestDetailsDialog(int requestId)
        {
            InitializeComponent();
            _requestId = requestId;
            LoadRequestDetails();
        }

        private void LoadRequestDetails()
        {
            try
            {
                using (var db = new MenuStolovayaDBEntities())
                {
                    var request = db.vw_Требования_накладные
                        .FirstOrDefault(r => r.id == _requestId);

                    if (request != null)
                    {
                        TitleText.Text = $"Требование накладная - {request.Номер}";
                        NumberText.Text = request.Номер;
                        DateText.Text = request.Дата_документа.ToString("dd.MM.yyyy");
                        TechnologistText.Text = request.Технолог;
                        StatusText.Text = request.Статус_требования;
                        CommentText.Text = request.Комментарий ?? "";

                        // Цвет статуса
                        if (StatusText.Text == "Подтверждено")
                            StatusText.Foreground = System.Windows.Media.Brushes.Green;
                        else if (StatusText.Text == "Ожидает")
                            StatusText.Foreground = System.Windows.Media.Brushes.Orange;
                        else
                            StatusText.Foreground = System.Windows.Media.Brushes.Red;
                    }

                    // Загрузка деталей
                    var details = db.vw_Требования_детали
                        .Where(d => d.Номер_требования == request.Номер)
                        .Select(d => new
                        {
                            d.Артикул,
                            d.Продукт,
                            d.Единица_измерения,
                            d.Количество,
                            d.Цена,
                            d.Сумма,
                            d.Остаток_на_складе
                        })
                        .ToList();

                    ProductsGrid.ItemsSource = details;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки деталей: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void PrintButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var printDialog = new System.Windows.Controls.PrintDialog();
                if (printDialog.ShowDialog() == true)
                {
                    printDialog.PrintVisual(this, $"Требование накладная {_requestId}");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при печати: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}