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
    public partial class AddEditDishWindow : Window
    {
        public DishModel Dish { get; private set; }
        public string WindowTitle { get; private set; }

        public AddEditDishWindow(Блюда dish = null)
        {
            InitializeComponent();
            DataContext = this;

            if (dish == null)
            {
                Dish = new DishModel
                {
                    Наименование = "",
                    Полное_наименование = "",
                    Выход_стандартный = 100,
                    Время_приготовления = 30,
                    Активно = true,
                    Дата_создания = DateTime.Now,
                    Кто_создал_id = Models.ThisUser.CurrentUser?.Id ?? 1
                };
                WindowTitle = "Добавление блюда";
            }
            else
            {
                Dish = new DishModel
                {
                    Id = dish.id,
                    Наименование = dish.Наименование,
                    Полное_наименование = dish.Полное_наименование ?? "",
                    Вид_блюда_id = dish.Вид_блюда_id,
                    Выход_стандартный = dish.Выход_стандартный ?? 100,
                    Время_приготовления = dish.Время_приготовления ?? 30,
                    Калорийность_расчетная = dish.Калорийность_расчетная,
                    Активно = dish.Активно ?? true,
                    Дата_создания = dish.Дата_создания ?? DateTime.Now,
                    Кто_создал_id = dish.Кто_создал_id ?? 1
                };
                WindowTitle = "Редактирование блюда";
            }

            LoadDishTypes();
        }

        private void LoadDishTypes()
        {
            try
            {
                using (var db = new MenuStolovayaDBEntities())
                {
                    var dishTypes = db.Виды_блюд
                        .Select(dt => new { Id = dt.id, Name = dt.Наименование })
                        .ToList();

                    DishTypeComboBox.ItemsSource = dishTypes;
                    DishTypeComboBox.DisplayMemberPath = "Name";
                    DishTypeComboBox.SelectedValuePath = "Id";

                    if (Dish.Id > 0 && Dish.Вид_блюда_id.HasValue)
                    {
                        DishTypeComboBox.SelectedValue = Dish.Вид_блюда_id.Value;
                    }
                    else if (dishTypes.Any())
                    {
                        // Выбираем первый вид по умолчанию
                        DishTypeComboBox.SelectedIndex = 0;
                        Dish.Вид_блюда_id = (int)DishTypeComboBox.SelectedValue;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке видов блюд: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(Dish.Наименование))
            {
                MessageBox.Show("Введите наименование блюда", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // ПРАВИЛЬНАЯ ПРОВЕРКА ВИДА БЛЮДА
            if (DishTypeComboBox.SelectedValue == null)
            {
                MessageBox.Show("Выберите вид блюда", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                DishTypeComboBox.Focus();
                return;
            }

            // Установка ID вида блюда
            Dish.Вид_блюда_id = (int)DishTypeComboBox.SelectedValue;

            if (Dish.Выход_стандартный <= 0)
            {
                MessageBox.Show("Выход должен быть больше 0", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (Dish.Время_приготовления <= 0)
            {
                MessageBox.Show("Время приготовления должно быть больше 0", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
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

        

        private void DishTypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DishTypeComboBox.SelectedValue != null && DishTypeComboBox.SelectedValue is int)
            {
                Dish.Вид_блюда_id = (int)DishTypeComboBox.SelectedValue;
            }
        }
    }
}