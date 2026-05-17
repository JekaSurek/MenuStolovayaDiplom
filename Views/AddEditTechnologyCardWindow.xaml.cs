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
using System.Text.RegularExpressions;

namespace MenuStolovaya.Views
{
    public partial class AddEditTechnologyCardWindow : Window
    {
        public TechnologyCardModel Card { get; private set; }
        public string WindowTitle { get; private set; }
        public bool IsNewCard { get; private set; }
        public bool CanChangeStatus { get; private set; }

        // Свойство для привязки выбранного блюда
        public int SelectedDishId
        {
            get => Card.Блюдо_id;
            set
            {
                if (Card.Блюдо_id != value)
                {
                    Card.Блюдо_id = value;
                    LoadSelectedDishInfo();
                }
            }
        }

        public AddEditTechnologyCardWindow(Технологические_карты card = null)
        {
            InitializeComponent();
            DataContext = this;

            if (card == null)
            {
                Card = new TechnologyCardModel
                {
                    Дата_создания = DateTime.Now,
                    Статус = "Черновик"
                };
                WindowTitle = "Создание технологической карты";
                IsNewCard = true;
                CanChangeStatus = true;
            }
            else
            {
                Card = new TechnologyCardModel
                {
                    Id = card.id,
                    Номер = card.Номер,
                    Блюдо_id = card.Блюдо_id,
                    Выход = card.Выход,
                    Технология_приготовления = card.Технология_приготовления,
                    Дата_создания = card.Дата_создания ?? DateTime.Now,
                    Статус = card.Статус,
                    Кто_утвердил_id = card.Кто_утвердил_id,
                    Дата_утверждения = card.Дата_утверждения
                };
                WindowTitle = "Редактирование технологической карты";
                IsNewCard = false;
                CanChangeStatus = ThisUser.IsTechnologist || ThisUser.IsAdmin;
            }

            LoadDishes();
        }

        private void LoadDishes()
        {
            try
            {
                using (var db = new MenuStolovayaDBEntities())
                {
                    var dishes = db.Блюда
                        .Where(d => d.Активно == true)
                        .Select(d => new
                        {
                            Id = d.id,
                            Name = d.Наименование,
                            StandardOutput = d.Выход_стандартный ?? 100
                        })
                        .ToList();

                    DishComboBox.ItemsSource = dishes;
                    DishComboBox.DisplayMemberPath = "Name";
                    DishComboBox.SelectedValuePath = "Id";

                    if (Card.Id > 0 && Card.Блюдо_id > 0)
                    {
                        DishComboBox.SelectedValue = Card.Блюдо_id;
                    }
                    else if (dishes.Any())
                    {
                        DishComboBox.SelectedIndex = 0;
                        SelectedDishId = (int)DishComboBox.SelectedValue;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке блюд: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadSelectedDishInfo()
        {
            if (DishComboBox.SelectedItem == null) return;

            try
            {
                using (var db = new MenuStolovayaDBEntities())
                {
                    var selectedDish = DishComboBox.SelectedItem as dynamic;
                    if (selectedDish != null)
                    {
                        int dishId = selectedDish.Id;
                        var dish = db.Блюда.Find(dishId);

                        if (dish != null)
                        {
                            // Показываем стандартный выход блюда
                            decimal standardOutput = dish.Выход_стандартный ?? 100;
                            StandardOutputText.Text = $"{standardOutput:N1} г";

                            // Если это новая техкарта и выход еще не задан - подставляем стандартный
                            if (IsNewCard && Card.Выход == 0)
                            {
                                Card.Выход = standardOutput;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке информации о блюде: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DishComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            LoadSelectedDishInfo();
        }

        // Валидация ввода чисел
        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex(@"^[0-9]*(?:\.[0-9]*)?$");
            e.Handled = !regex.IsMatch((sender as TextBox).Text.Insert((sender as TextBox).SelectionStart, e.Text));
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            // Валидация
            if (Card.Блюдо_id <= 0)
            {
                MessageBox.Show("Выберите блюдо", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                DishComboBox.Focus();
                return;
            }

            if (Card.Выход <= 0)
            {
                MessageBox.Show("Выход должен быть больше 0", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                OutputTextBox.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(Card.Технология_приготовления))
            {
                MessageBox.Show("Заполните технологию приготовления", "Ошибка",
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
    }
}