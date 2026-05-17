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
    public partial class AddEditMenuWindow : Window
    {
        public DailyMenuModel Menu { get; private set; }
        public string WindowTitle { get; private set; }
        public bool CanChangeStatus { get; private set; }

        public AddEditMenuWindow(Меню_на_день menu = null)
        {
            InitializeComponent();
            DataContext = this;

            if (menu == null)
            {
                Menu = new DailyMenuModel
                {
                    Дата = DateTime.Today,
                    Ответственный_id = ThisUser.CurrentUser?.Id ?? 1,
                    Дата_составления = DateTime.Now,
                    Статус = "Черновик"
                };
                WindowTitle = "Создание меню на день";
                CanChangeStatus = true;
            }
            else
            {
                Menu = new DailyMenuModel
                {
                    Id = menu.id,
                    Дата = menu.Дата,
                    Ответственный_id = menu.Ответственный_id,
                    Дата_составления = menu.Дата_составления ?? DateTime.Now,
                    Калорийность_общая = menu.Калорийность_общая,
                    Статус = menu.Статус
                };
                WindowTitle = "Редактирование меню";
                CanChangeStatus = ThisUser.IsTechnologist || ThisUser.IsAdmin;
            }

            LoadResponsible();
        }

        private void LoadResponsible()
        {
            try
            {
                using (var db = new MenuStolovayaDBEntities())
                {
                    var responsible = db.Пользователи
                        .FirstOrDefault(u => u.id == Menu.Ответственный_id);

                    if (responsible != null)
                    {
                        ResponsibleText.Text = $"{responsible.Фамилия} {responsible.Имя}";
                    }
                    else
                    {
                        ResponsibleText.Text = ThisUser.CurrentUser?.FullName ?? "Неизвестно";
                        Menu.Ответственный_id = ThisUser.CurrentUser?.Id ?? 1;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке данных: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (Menu.Дата < DateTime.Today.AddDays(-1))
            {
                MessageBox.Show("Нельзя создавать меню на прошедшие даты", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                DatePicker.Focus();
                return;
            }

            // При сохранении меню пересчитываем его калорийность
            if (Menu.Id > 0) // Если меню редактируется, а не создается
            {
                try
                {
                    using (var db = new MenuStolovayaDBEntities())
                    {
                        // Рассчитываем и обновляем калорийность
                        decimal totalCalories = db.Строки_меню
                            .Where(sm => sm.Меню_id == Menu.Id)
                            .Join(db.Блюда,
                                sm => sm.Блюдо_id,
                                b => b.id,
                                (sm, b) => new
                                {
                                    Порций = sm.Количество_порций ?? 1,
                                    ВыходПорции = sm.Выход_на_порцию ?? b.Выход_стандартный ?? 100,
                                    Калорийность = b.Калорийность_расчетная ?? 0
                                })
                            .ToList()
                            .Sum(item => (item.Калорийность / 100m * item.ВыходПорции) * item.Порций);

                        Menu.Калорийность_общая = totalCalories;
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Ошибка при расчете калорийности: {ex.Message}");
                }
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