using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using MenuStolovaya.Models;

namespace MenuStolovaya.Services
{
    public class ProductService
    {
        public List<ProductDisplay> GetProducts(string filter = "")
        {
            try
            {
                using (var db = new MenuStolovayaDBEntities())
                {
                    var query = db.Продукты
                        .Include("Категории_продуктов")
                        .Include("Пользователи")
                        .Where(p => p.Активен == true); // Только активные продукты

                    if (!string.IsNullOrWhiteSpace(filter))
                    {
                        query = query.Where(p =>
                            p.Наименование.Contains(filter) ||
                            p.Артикул.Contains(filter) ||
                            (p.Категории_продуктов != null && p.Категории_продуктов.Наименование.Contains(filter)));
                    }

                    return query.Select(p => new ProductDisplay
                    {
                        Id = p.id,
                        Артикул = p.Артикул,
                        Наименование = p.Наименование,
                        Категория = p.Категории_продуктов != null ? p.Категории_продуктов.Наименование : "Без категории",
                        Единица_измерения = p.Единица_измерения,
                        Цена = p.Цена ?? 0,
                        Утверждена_цена = p.Утверждена_цена ?? false,
                        Калорийность = p.Калорийность
                    }).ToList();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке продуктов: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return new List<ProductDisplay>();
            }
        }

        public bool AddProduct(ProductModel product)
        {
            try
            {
                using (var db = new MenuStolovayaDBEntities())
                {
                    // Проверка на дубликаты по артикулу
                    if (!string.IsNullOrWhiteSpace(product.Артикул) &&
                        db.Продукты.Any(p => p.Артикул == product.Артикул && p.Активен == true))
                    {
                        MessageBox.Show("Продукт с таким артикулом уже существует", "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                        return false;
                    }

                    var newProduct = new Продукты
                    {
                        Артикул = product.Артикул,
                        Наименование = product.Наименование,
                        Категория_id = product.Категория_id,
                        Единица_измерения = product.Единица_измерения,
                        Потери_холодной_обработки = product.Потери_холодной_обработки,
                        Потери_горячей_обработки = product.Потери_горячей_обработки,
                        Белки = product.Белки,
                        Жиры = product.Жиры,
                        Углеводы = product.Углеводы,
                        Калорийность = product.Калорийность,
                        Цена = product.Цена,
                        Утверждена_цена = false, // При создании цена не утверждена
                        Активен = true // Продукт активен по умолчанию
                    };

                    db.Продукты.Add(newProduct);
                    db.SaveChanges();
                    return true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при добавлении продукта: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        public bool UpdateProduct(ProductModel product)
        {
            try
            {
                using (var db = new MenuStolovayaDBEntities())
                {
                    var existingProduct = db.Продукты.Find(product.Id);
                    if (existingProduct != null && existingProduct.Активен == true)
                    {
                        // Проверка на дубликаты по артикулу (исключая текущий продукт)
                        if (!string.IsNullOrWhiteSpace(product.Артикул) &&
                            db.Продукты.Any(p => p.Артикул == product.Артикул && p.id != product.Id && p.Активен == true))
                        {
                            MessageBox.Show("Продукт с таким артикулом уже существует", "Ошибка",
                                MessageBoxButton.OK, MessageBoxImage.Error);
                            return false;
                        }

                        // Рассчитываем калорийность из БЖУ, если не указана
                        if (!product.Калорийность.HasValue && product.Белки.HasValue &&
                            product.Жиры.HasValue && product.Углеводы.HasValue)
                        {
                            product.Калорийность = product.Белки.Value * 4 +
                                                   product.Жиры.Value * 9 +
                                                   product.Углеводы.Value * 4;
                        }

                        existingProduct.Артикул = product.Артикул;
                        existingProduct.Наименование = product.Наименование;
                        existingProduct.Категория_id = product.Категория_id;
                        existingProduct.Единица_измерения = product.Единица_измерения;
                        existingProduct.Потери_холодной_обработки = product.Потери_холодной_обработки;
                        existingProduct.Потери_горячей_обработки = product.Потери_горячей_обработки;
                        existingProduct.Белки = product.Белки;
                        existingProduct.Жиры = product.Жиры;
                        existingProduct.Углеводы = product.Углеводы;
                        existingProduct.Калорийность = product.Калорийность;
                        existingProduct.Цена = product.Цена;
                        // Утверждена_цена не изменяем - это делает бухгалтер

                        db.SaveChanges();
                        return true;
                    }
                    return false;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при обновлении продукта: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        public bool DeleteProduct(int productId)
        {
            try
            {
                using (var db = new MenuStolovayaDBEntities())
                {
                    var product = db.Продукты.Find(productId);
                    if (product != null && product.Активен == true)
                    {
                        // Проверка, используется ли продукт в рецептурах
                        var usedInRecipes = db.Рецептуры.Any(r => r.Продукт_id == productId);
                        if (usedInRecipes)
                        {
                            MessageBox.Show("Нельзя удалить продукт, который используется в рецептурах. Сначала удалите его из всех рецептур.",
                                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                            return false;
                        }

                        // Мягкое удаление - делаем продукт неактивным
                        product.Активен = false;
                        db.SaveChanges();
                        return true;
                    }
                    return false;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при удалении продукта: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }
    }

    public class ProductDisplay
    {
        public int Id { get; set; }
        public string Артикул { get; set; }
        public string Наименование { get; set; }
        public string Категория { get; set; }
        public string Единица_измерения { get; set; }
        public decimal Цена { get; set; }
        public bool Утверждена_цена { get; set; }
        public decimal? Калорийность { get; set; }
    }
}