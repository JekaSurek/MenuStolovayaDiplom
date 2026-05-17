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
    public partial class AddEditDishTypeWindow : Window
    {
        public DishTypeModel DishType { get; private set; }
        public string WindowTitle { get; private set; }

        public AddEditDishTypeWindow(Виды_блюд dishType = null)
        {
            InitializeComponent();
            DataContext = this;

            if (dishType == null)
            {
                DishType = new DishTypeModel
                {
                    Наименование = "",
                    Описание = ""
                };
                WindowTitle = "Добавление вида блюда";
            }
            else
            {
                DishType = new DishTypeModel
                {
                    Id = dishType.id,
                    Наименование = dishType.Наименование,
                    Описание = dishType.Описание ?? ""
                };
                WindowTitle = "Редактирование вида блюда";
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(DishType.Наименование))
            {
                MessageBox.Show("Введите наименование вида блюда", "Ошибка",
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