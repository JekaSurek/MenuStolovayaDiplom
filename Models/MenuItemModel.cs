using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MenuStolovaya.Models
{
    public class MenuItemModel
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey("Меню")]
        public int Меню_id { get; set; }
        public virtual DailyMenuModel Меню { get; set; }

        [ForeignKey("Блюдо")]
        public int Блюдо_id { get; set; }
        public virtual DishModel Блюдо { get; set; }

        public int Количество_порций { get; set; }
        public decimal? Выход_на_порцию { get; set; }
        public string Время_подачи { get; set; }
        public int Порядок_подачи { get; set; }
    }
}