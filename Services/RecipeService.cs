using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using MenuStolovaya.Models;

namespace MenuStolovaya.Services
{
    public class RecipeService
    {
        public List<RecipeDisplay> GetRecipes(int technologyCardId)
        {
            try
            {
                using (var db = new MenuStolovayaDBEntities())
                {
                    var recipesData = db.Рецептуры
                        .Where(r => r.Технологическая_карта_id == technologyCardId)
                        .Join(db.Продукты,
                            r => r.Продукт_id,
                            p => p.id,
                            (r, p) => new
                            {
                                Id = r.id,
                                Продукт_id = p.id,
                                Продукт = p.Наименование,
                                Артикул = p.Артикул,
                                Единица_измерения = p.Единица_измерения,
                                Количество_брутто = r.Количество_брутто,
                                Количество_нетто = r.Количество_нетто,
                                Порядок_закладки = r.Порядок_закладки,
                                Потери_холодной = p.Потери_холодной_обработки,
                                Потери_горячей = p.Потери_горячей_обработки
                            })
                        .ToList();

                    var result = new List<RecipeDisplay>();

                    foreach (var r in recipesData)
                    {
                        result.Add(new RecipeDisplay
                        {
                            Id = r.Id,
                            Продукт_id = r.Продукт_id,
                            Продукт = r.Продукт,
                            Артикул = r.Артикул,
                            Единица_измерения = r.Единица_измерения,
                            Количество_брутто = r.Количество_брутто,
                            Количество_нетто = r.Количество_нетто ?? r.Количество_брутто,
                            Порядок_закладки = r.Порядок_закладки ?? 0,
                            Потери_холодной = r.Потери_холодной ?? 0,
                            Потери_горячей = r.Потери_горячей ?? 0
                        });
                    }

                    return result.OrderBy(r => r.Порядок_закладки).ToList();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке рецептуры: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return new List<RecipeDisplay>();
            }
        }

        public bool AddRecipe(RecipeModel recipe)
        {
            try
            {
                using (var db = new MenuStolovayaDBEntities())
                {
                    // Проверяем, не добавлен ли уже этот продукт
                    var existing = db.Рецептуры
                        .FirstOrDefault(r => r.Технологическая_карта_id == recipe.Технологическая_карта_id &&
                                           r.Продукт_id == recipe.Продукт_id);

                    if (existing != null)
                    {
                        MessageBox.Show("Этот продукт уже добавлен в рецептуру", "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                        return false;
                    }

                    var newRecipe = new Рецептуры
                    {
                        Технологическая_карта_id = recipe.Технологическая_карта_id,
                        Продукт_id = recipe.Продукт_id,
                        Количество_брутто = recipe.Количество_брутто,
                        Порядок_закладки = recipe.Порядок_закладки
                    };

                    db.Рецептуры.Add(newRecipe);
                    db.SaveChanges();

                    // ОБНОВЛЯЕМ ВЫХОД И КАЛОРИЙНОСТЬ БЛЮДА
                    CalorieCalculator.UpdateDishCalculations(recipe.Технологическая_карта_id);

                    return true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при добавлении продукта в рецептуру: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        public bool UpdateRecipe(RecipeModel recipe)
        {
            try
            {
                using (var db = new MenuStolovayaDBEntities())
                {
                    var existingRecipe = db.Рецептуры.Find(recipe.Id);
                    if (existingRecipe != null)
                    {
                        existingRecipe.Количество_брутто = recipe.Количество_брутто;
                        existingRecipe.Порядок_закладки = recipe.Порядок_закладки;
                        db.SaveChanges();

                        // ОБНОВЛЯЕМ ВЫХОД И КАЛОРИЙНОСТЬ БЛЮДА
                        CalorieCalculator.UpdateDishCalculations(existingRecipe.Технологическая_карта_id);

                        return true;
                    }
                    return false;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при обновлении рецептуры: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        public bool DeleteRecipe(int recipeId)
        {
            try
            {
                using (var db = new MenuStolovayaDBEntities())
                {
                    var recipe = db.Рецептуры.Find(recipeId);
                    if (recipe != null)
                    {
                        int techCardId = recipe.Технологическая_карта_id;

                        db.Рецептуры.Remove(recipe);
                        db.SaveChanges();

                        // ОБНОВЛЯЕМ ВЫХОД И КАЛОРИЙНОСТЬ БЛЮДА
                        CalorieCalculator.UpdateDishCalculations(techCardId);

                        return true;
                    }
                    return false;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при удалении из рецептуры: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        public bool UpdateRecipeOrder(int technologyCardId)
        {
            try
            {
                using (var db = new MenuStolovayaDBEntities())
                {
                    var recipes = db.Рецептуры
                        .Where(r => r.Технологическая_карта_id == technologyCardId)
                        .ToList()
                        .OrderBy(r => r.Порядок_закладки ?? 0)
                        .ToList();

                    for (int i = 0; i < recipes.Count; i++)
                    {
                        recipes[i].Порядок_закладки = i + 1;
                    }

                    db.SaveChanges();
                    return true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при обновлении порядка: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        public bool UpdateRecipeAndCalories(RecipeModel recipe)
        {
            try
            {
                using (var db = new MenuStolovayaDBEntities())
                {
                    var existingRecipe = db.Рецептуры.Find(recipe.Id);
                    if (existingRecipe != null)
                    {
                        existingRecipe.Количество_брутто = recipe.Количество_брутто;
                        existingRecipe.Порядок_закладки = recipe.Порядок_закладки;
                        db.SaveChanges();

                        // ОБНОВЛЯЕМ ВЫХОД И КАЛОРИЙНОСТЬ БЛЮДА
                        CalorieCalculator.UpdateDishCalculations(existingRecipe.Технологическая_карта_id);

                        return true;
                    }
                    return false;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при обновлении рецептуры: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }
    }

    public class RecipeDisplay
    {
        public int Id { get; set; }
        public int Продукт_id { get; set; }
        public string Продукт { get; set; }
        public string Артикул { get; set; }
        public string Единица_измерения { get; set; }
        public decimal Количество_брутто { get; set; }
        public decimal Количество_нетто { get; set; }
        public int Порядок_закладки { get; set; }
        public decimal Потери_холодной { get; set; }
        public decimal Потери_горячей { get; set; }
    }
}