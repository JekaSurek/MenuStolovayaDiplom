using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MenuStolovaya.Models
{
    public class TechnologyCardModel
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Номер { get; set; }

        [ForeignKey("Блюдо")]
        public int Блюдо_id { get; set; }
        public virtual DishModel Блюдо { get; set; }

        [Required]
        public decimal Выход { get; set; }

        public string Технология_приготовления { get; set; }
        public DateTime Дата_создания { get; set; }
        public string Статус { get; set; }

        [ForeignKey("Кто_утвердил")]
        public int? Кто_утвердил_id { get; set; }
        public virtual UserModel Кто_утвердил { get; set; }

        public DateTime? Дата_утверждения { get; set; }

        // Навигационное свойство
        public virtual ICollection<RecipeModel> Рецептуры { get; set; }
    }
}