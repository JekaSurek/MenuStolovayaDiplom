using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using MenuStolovaya.Models;

namespace MenuStolovaya.Services
{
    public class DishTypeService
    {
        public List<DishTypeModel> GetDishTypes(string filter = "")
        {
            try
            {
                using (var db = new MenuStolovayaDBEntities())
                {
                    var query = db.Виды_блюд.AsQueryable();

                    if (!string.IsNullOrWhiteSpace(filter))
                    {
                        query = query.Where(dt => dt.Наименование.Contains(filter));
                    }

                    return query.Select(dt => new DishTypeModel
                    {
                        Id = dt.id,
                        Наименование = dt.Наименование,
                        Описание = dt.Описание
                    }).ToList();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке видов блюд: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return new List<DishTypeModel>();
            }
        }

        public bool AddDishType(DishTypeModel dishType)
        {
            try
            {
                using (var db = new MenuStolovayaDBEntities())
                {
                    // Проверка на дубликаты
                    if (db.Виды_блюд.Any(dt => dt.Наименование == dishType.Наименование))
                    {
                        MessageBox.Show("Вид блюда с таким наименованием уже существует", "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                        return false;
                    }

                    var newDishType = new Виды_блюд
                    {
                        Наименование = dishType.Наименование,
                        Описание = dishType.Описание
                    };

                    db.Виды_блюд.Add(newDishType);
                    db.SaveChanges();
                    return true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при добавлении вида блюда: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        public bool UpdateDishType(DishTypeModel dishType)
        {
            try
            {
                using (var db = new MenuStolovayaDBEntities())
                {
                    var existingDishType = db.Виды_блюд.Find(dishType.Id);
                    if (existingDishType != null)
                    {
                        // Проверка на дубликаты (исключая текущий вид блюда)
                        if (db.Виды_блюд.Any(dt => dt.Наименование == dishType.Наименование && dt.id != dishType.Id))
                        {
                            MessageBox.Show("Вид блюда с таким наименованием уже существует", "Ошибка",
                                MessageBoxButton.OK, MessageBoxImage.Error);
                            return false;
                        }

                        existingDishType.Наименование = dishType.Наименование;
                        existingDishType.Описание = dishType.Описание;
                        db.SaveChanges();
                        return true;
                    }
                    return false;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при обновлении вида блюда: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        public bool DeleteDishType(int dishTypeId)
        {
            try
            {
                using (var db = new MenuStolovayaDBEntities())
                {
                    var dishType = db.Виды_блюд.Find(dishTypeId);
                    if (dishType != null)
                    {
                        // Проверка, что вид блюда не используется в активных блюдах
                        var dishesUsingType = db.Блюда
                            .Where(d => d.Вид_блюда_id == dishTypeId && d.Активно == true)
                            .ToList();

                        if (dishesUsingType.Any())
                        {
                            var dishNames = string.Join(", ", dishesUsingType.Take(3).Select(d => d.Наименование));
                            var message = dishesUsingType.Count > 3
                                ? $"Вид блюда используется в {dishesUsingType.Count} активных блюдах, включая: {dishNames}..."
                                : $"Вид блюда используется в активных блюдах: {dishNames}";

                            MessageBox.Show($"Нельзя удалить вид блюда.\n{message}", "Ошибка",
                                MessageBoxButton.OK, MessageBoxImage.Error);
                            return false;
                        }

                        // Для неактивных блюд с этим видом блюда сбрасываем вид на NULL
                        var inactiveDishesWithType = db.Блюда
                            .Where(d => d.Вид_блюда_id == dishTypeId && d.Активно == false)
                            .ToList();

                        foreach (var dish in inactiveDishesWithType)
                        {
                            dish.Вид_блюда_id = null;
                        }

                        db.Виды_блюд.Remove(dishType);
                        db.SaveChanges();
                        return true;
                    }
                    return false;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при удалении вида блюда: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }
    }
}