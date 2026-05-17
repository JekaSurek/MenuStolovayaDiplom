using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using MenuStolovaya.Models;

namespace MenuStolovaya.Views
{
    public partial class EditCalcCardDialog : Window
    {
        private int _calcCardId;
        private decimal _currentMarkup;

        public EditCalcCardDialog(int calcCardId)
        {
            InitializeComponent();
            _calcCardId = calcCardId;
            LoadCalcCardData();
            InitializeEventHandlers();
        }

        private void LoadCalcCardData()
        {
            try
            {
                using (var db = new MenuStolovayaDBEntities())
                {
                    var calcCard = db.Калькуляционные_карточки
                        .Include("Технологические_карты")
                        .Include("Технологические_карты.Блюда")
                        .FirstOrDefault(cc => cc.id == _calcCardId);

                    if (calcCard == null)
                    {
                        MessageBox.Show("Калькуляционная карточка не найдена",
                            "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        this.Close();
                        return;
                    }

                    // Запрещаем редактирование утвержденных карточек
                    if (calcCard.Статус == "Утверждена")
                    {
                        MessageBox.Show("Утвержденную калькуляционную карточку нельзя редактировать",
                            "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        this.Close();
                        return;
                    }

                    CardNumberText.Text = calcCard.Номер;
                    DishText.Text = calcCard.Технологические_карты?.Блюда?.Наименование ?? "Неизвестно";
                    OutputText.Text = $"{calcCard.Технологические_карты?.Выход ?? 0:N1} г";

                    _currentMarkup = calcCard.Процент_наценки ?? 150;
                    MarkupSlider.Value = (double)_currentMarkup;

                    UpdateCostDisplay(calcCard.Себестоимость ?? 0);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке данных: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                this.Close();
            }
        }

        private void InitializeEventHandlers()
        {
            MarkupSlider.ValueChanged += MarkupSlider_ValueChanged;
        }

        private void MarkupSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            using (var db = new MenuStolovayaDBEntities())
            {
                var calcCard = db.Калькуляционные_карточки.Find(_calcCardId);
                if (calcCard != null && calcCard.Себестоимость.HasValue)
                {
                    UpdateCostDisplay(calcCard.Себестоимость.Value);
                }
            }
        }

        private void UpdateCostDisplay(decimal cost)
        {
            decimal markup = (decimal)MarkupSlider.Value;

            CostText.Text = $"{cost:N2} руб.";

            decimal price = cost * (1 + markup / 100);
            PriceText.Text = $"{price:N2} руб.";

            decimal foodCost = cost > 0 ? (cost / price) * 100 : 0;
            FoodCostText.Text = $"{foodCost:N2}%";

            string margin;

            if (foodCost <= 25)
            {
                margin = "Высокомаржинальное";
            }
            else if (foodCost <= 35)
            {
                margin = "Среднемаржинальное";
            }
            else
            {
                margin = "Низкомаржинальное";
            }

            MarginText.Text = margin;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                using (var db = new MenuStolovayaDBEntities())
                {
                    var calcCard = db.Калькуляционные_карточки.Find(_calcCardId);
                    if (calcCard == null) return;

                    decimal newMarkup = (decimal)MarkupSlider.Value;

                    // Обновляем только наценку и пересчитываем цену
                    calcCard.Процент_наценки = newMarkup;

                    if (calcCard.Себестоимость.HasValue)
                    {
                        calcCard.Цена_реализации = calcCard.Себестоимость.Value * (1 + newMarkup / 100);
                    }

                    db.SaveChanges();
                }

                this.DialogResult = true;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении изменений: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}