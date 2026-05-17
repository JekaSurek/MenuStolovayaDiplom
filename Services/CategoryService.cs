using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using MenuStolovaya.Models;

namespace MenuStolovaya.Services
{
    public class CategoryService
    {
        public List<CategoryModel> GetCategories(string filter = "")
        {
            try
            {
                using (var db = new MenuStolovayaDBEntities())
                {
                    var query = db.Категории_продуктов.AsQueryable();

                    if (!string.IsNullOrWhiteSpace(filter))
                    {
                        query = query.Where(c => c.Наименование.Contains(filter));
                    }

                    return query.Select(c => new CategoryModel
                    {
                        Id = c.id,
                        Наименование = c.Наименование,
                        Описание = c.Описание
                    }).ToList();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке категорий: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return new List<CategoryModel>();
            }
        }

        public bool AddCategory(CategoryModel category)
        {
            try
            {
                using (var db = new MenuStolovayaDBEntities())
                {
                    var newCategory = new Категории_продуктов
                    {
                        Наименование = category.Наименование,
                        Описание = category.Описание
                    };

                    db.Категории_продуктов.Add(newCategory);
                    db.SaveChanges();
                    return true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при добавлении категории: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        public bool UpdateCategory(CategoryModel category)
        {
            try
            {
                using (var db = new MenuStolovayaDBEntities())
                {
                    var existingCategory = db.Категории_продуктов.Find(category.Id);
                    if (existingCategory != null)
                    {
                        existingCategory.Наименование = category.Наименование;
                        existingCategory.Описание = category.Описание;
                        db.SaveChanges();
                        return true;
                    }
                    return false;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при обновлении категории: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        public bool DeleteCategory(int categoryId)
        {
            try
            {
                using (var db = new MenuStolovayaDBEntities())
                {
                    var category = db.Категории_продуктов.Find(categoryId);
                    if (category != null)
                    {
                        // Проверка, что категория не используется в продуктах
                        if (db.Продукты.Any(p => p.Категория_id == categoryId))
                        {
                            MessageBox.Show("Нельзя удалить категорию, которая используется в продуктах",
                                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                            return false;
                        }

                        db.Категории_продуктов.Remove(category);
                        db.SaveChanges();
                        return true;
                    }
                    return false;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при удалении категории: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }
    }
}