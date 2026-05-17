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
    public partial class AddEditCategoryWindow : Window
    {
        public CategoryModel Category { get; private set; }
        public string WindowTitle { get; private set; }

        public AddEditCategoryWindow(Категории_продуктов category = null)
        {
            InitializeComponent();
            DataContext = this;

            if (category == null)
            {
                Category = new CategoryModel
                {
                    Наименование = "",
                    Описание = ""
                };
                WindowTitle = "Добавление категории";
            }
            else
            {
                Category = new CategoryModel
                {
                    Id = category.id,
                    Наименование = category.Наименование,
                    Описание = category.Описание ?? ""
                };
                WindowTitle = "Редактирование категории";
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(Category.Наименование))
            {
                MessageBox.Show("Введите наименование категории", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                NameTextBox.Focus();
                return;
            }

            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}