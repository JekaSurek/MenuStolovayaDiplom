using MenuStolovaya.Models;
using MenuStolovaya.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace MenuStolovaya.Views
{
    public partial class CreateCalcCardDialog : Window
    {
        public bool IsCreated { get; private set; }
        private List<AccountantTechCardDisplay> _techCards;

        public CreateCalcCardDialog()
        {
            InitializeComponent();
            LoadTechCards();
        }

        private void LoadTechCards()
        {
            try
            {
                using (var db = new MenuStolovayaDBEntities())
                {
                    // Получаем только утвержденные технологические карты без калькуляционных карточек
                    _techCards = db.Технологические_карты
                        .Where(tc => tc.Статус == "Утверждена" &&
                                   !db.Калькуляционные_карточки.Any(cc => cc.Технологическая_карта_id == tc.id))
                        .Join(db.Блюда,
                            tc => tc.Блюдо_id,
                            b => b.id,
                            (tc, b) => new AccountantTechCardDisplay
                            {
                                Id = tc.id,
                                Номер = tc.Номер,
                                Блюдо = b.Наименование,
                                Выход = tc.Выход
                            })
                        .ToList();

                    TechCardCombo.ItemsSource = _techCards;
                    if (_techCards.Any())
                    {
                        TechCardCombo.SelectedIndex = 0;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке технологических карт: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CreateButton_Click(object sender, RoutedEventArgs e)
        {
            if (TechCardCombo.SelectedItem == null)
            {
                MessageBox.Show("Выберите технологическую карту",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var selectedTechCard = (AccountantTechCardDisplay)TechCardCombo.SelectedItem;
                decimal markup = decimal.Parse(MarkupTextBox.Text);

                using (var db = new MenuStolovayaDBEntities())
                {
                    // Проверяем, нет ли уже калькуляционной карточки для этой техкарты
                    var existingCard = db.Калькуляционные_карточки
                        .FirstOrDefault(cc => cc.Технологическая_карта_id == selectedTechCard.Id);

                    if (existingCard != null)
                    {
                        MessageBox.Show($"Для этой технологической карты уже существует калькуляционная карточка:\n" +
                            $"Номер: {existingCard.Номер}\nСтатус: {existingCard.Статус}",
                            "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    // Создаем калькуляционную карточку через сервис
                    var calcCardService = new CalcCardService();
                    if (calcCardService.CreateCalcCardFromTechCard(selectedTechCard.Id, markup))
                    {
                        IsCreated = true;
                        this.DialogResult = true;
                        this.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при создании калькуляционной карточки: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void TechCardCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (TechCardCombo.SelectedItem is AccountantTechCardDisplay selectedTechCard)
            {
                try
                {
                    using (var db = new MenuStolovayaDBEntities())
                    {
                        var existingCard = db.Калькуляционные_карточки
                            .FirstOrDefault(cc => cc.Технологическая_карта_id == selectedTechCard.Id);

                        if (existingCard != null)
                        {
                            MessageBox.Show($"Внимание! Для этой технологической карты уже существует калькуляционная карточка:\n" +
                                $"Номер: {existingCard.Номер}\nСтатус: {existingCard.Статус}\n" +
                                $"Вы можете редактировать существующую карточку.",
                                "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                        }
                    }
                }
                catch { /* Игнорируем ошибки при загрузке */ }
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}