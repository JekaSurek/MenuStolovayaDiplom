using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using MenuStolovaya.Models;

namespace MenuStolovaya.Services
{
    public class ProductPriceService
    {
        public List<ProductPriceDisplay> GetProducts(string filter = "")
        {
            try
            {
                using (var db = new MenuStolovayaDBEntities())
                {
                    var query = db.Продукты
                        .Include("Категории_продуктов")
                        .Include("Пользователи")
                        .Where(p => p.Активен == true)
                        .AsQueryable();

                    if (!string.IsNullOrWhiteSpace(filter))
                    {
                        query = query.Where(p =>
                            p.Наименование.Contains(filter) ||
                            p.Артикул.Contains(filter) ||
                            (p.Категории_продуктов != null && p.Категории_продуктов.Наименование.Contains(filter)));
                    }

                    // Получаем данные из базы
                    var products = query.ToList();

                    // Теперь обрабатываем в памяти, используя String.Format
                    var result = new List<ProductPriceDisplay>();

                    foreach (var p in products)
                    {
                        string кто_утвердил = "Не утверждена";
                        if (p.Пользователи != null)
                        {
                            кто_утвердил = $"{p.Пользователи.Фамилия} {p.Пользователи.Имя}";
                        }

                        string категория = "Без категории";
                        if (p.Категории_продуктов != null)
                        {
                            категория = p.Категории_продуктов.Наименование;
                        }

                        result.Add(new ProductPriceDisplay
                        {
                            Id = p.id,
                            Артикул = p.Артикул ?? "Не указан",
                            Наименование = p.Наименование,
                            Категория = категория,
                            Единица_измерения = p.Единица_измерения,
                            Цена = p.Цена ?? 0,
                            Утверждена_цена = p.Утверждена_цена ?? false,
                            Дата_утверждения_цены = p.Дата_утверждения_цены,
                            Кто_утвердил_цену = кто_утвердил
                        });
                    }

                    return result;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке продуктов: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return new List<ProductPriceDisplay>();
            }
        }

        public bool ApproveProductPrice(int productId, decimal price)
        {
            try
            {
                using (var db = new MenuStolovayaDBEntities())
                {
                    var product = db.Продукты.Find(productId);
                    if (product == null)
                    {
                        MessageBox.Show("Продукт не найден",
                            "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        return false;
                    }

                    product.Цена = price;
                    product.Утверждена_цена = true;
                    product.Кто_утвердил_цену_id = ThisUser.CurrentUser?.Id;
                    product.Дата_утверждения_цены = DateTime.Now;

                    db.SaveChanges();

                    // После утверждения цены пересчитываем все калькуляционные карточки, где используется этот продукт
                    RecalculateCalcCardsWithProduct(productId);

                    return true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при утверждении цены продукта: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        public bool UpdateProductPrice(int productId, decimal newPrice)
        {
            try
            {
                using (var db = new MenuStolovayaDBEntities())
                {
                    var product = db.Продукты.Find(productId);
                    if (product == null)
                    {
                        MessageBox.Show("Продукт не найден",
                            "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        return false;
                    }

                    product.Цена = newPrice;
                    product.Утверждена_цена = false; // Сбрасываем статус утверждения
                    product.Кто_утвердил_цену_id = null;
                    product.Дата_утверждения_цены = null;

                    db.SaveChanges();
                    return true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при изменении цены продукта: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        private void RecalculateCalcCardsWithProduct(int productId)
        {
            try
            {
                using (var db = new MenuStolovayaDBEntities())
                {
                    // Находим все калькуляционные карточки, где используется этот продукт
                    var calcCards = db.Строки_калькуляции
                        .Where(cl => cl.Продукт_id == productId)
                        .Select(cl => cl.Калькуляционная_карточка_id)
                        .Distinct()
                        .ToList();

                    foreach (var calcCardId in calcCards)
                    {
                        var calcCard = db.Калькуляционные_карточки.Find(calcCardId);
                        if (calcCard != null && calcCard.Статус != "Утверждена")
                        {
                            // Пересчитываем строки калькуляции для этой карточки
                            RecalcCalcCardLines(calcCardId);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при пересчете калькуляционных карточек: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void RecalcCalcCardLines(int calcCardId)
        {
            try
            {
                using (var db = new MenuStolovayaDBEntities())
                {
                    var calcLines = db.Строки_калькуляции
                        .Where(cl => cl.Калькуляционная_карточка_id == calcCardId)
                        .ToList();

                    decimal totalCost = 0;

                    foreach (var line in calcLines)
                    {
                        var product = db.Продукты.Find(line.Продукт_id);
                        if (product != null && product.Цена.HasValue)
                        {
                            line.Цена_за_единицу = product.Цена.Value;
                            line.Сумма = line.Норма_расхода * product.Цена.Value;
                            totalCost += line.Сумма;
                        }
                    }

                    // Обновляем себестоимость в карточке
                    var calcCard = db.Калькуляционные_карточки.Find(calcCardId);
                    if (calcCard != null)
                    {
                        calcCard.Себестоимость = totalCost;

                        // Пересчитываем цену реализации
                        if (calcCard.Процент_наценки.HasValue)
                        {
                            calcCard.Цена_реализации = totalCost * (1 + calcCard.Процент_наценки.Value / 100);
                        }
                    }

                    db.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при пересчете калькуляционной карточки: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    public class ProductPriceDisplay
    {
        public int Id { get; set; }
        public string Артикул { get; set; }
        public string Наименование { get; set; }
        public string Категория { get; set; }
        public string Единица_измерения { get; set; }
        public decimal Цена { get; set; }
        public bool Утверждена_цена { get; set; }
        public DateTime? Дата_утверждения_цены { get; set; }
        public string Кто_утвердил_цену { get; set; }
    }
}