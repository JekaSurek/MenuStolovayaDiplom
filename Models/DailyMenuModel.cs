using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MenuStolovaya.Models
{
    public class DailyMenuModel
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public DateTime Дата { get; set; }

        [ForeignKey("Ответственный")]
        public int Ответственный_id { get; set; }
        public virtual UserModel Ответственный { get; set; }

        public DateTime Дата_составления { get; set; }
        public decimal? Калорийность_общая { get; set; }
        public string Статус { get; set; }

        // Навигационное свойство
        public virtual ICollection<MenuItemModel> Строки_меню { get; set; }
    }
}