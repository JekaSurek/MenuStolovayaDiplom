using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MenuStolovaya.Models
{
    public class DishModel
    {
        [Key]
        public int Id { get; set; }
        public string Наименование { get; set; }
        public string Полное_наименование { get; set; }

        [ForeignKey("Вид_блюда")]
        public int? Вид_блюда_id { get; set; }
        public virtual DishTypeModel Вид_блюда { get; set; }

        public decimal Выход_стандартный { get; set; }
        public int Время_приготовления { get; set; }
        public decimal? Калорийность_расчетная { get; set; }
        public bool Активно { get; set; }
        public DateTime Дата_создания { get; set; }

        [ForeignKey("Кто_создал")]
        public int Кто_создал_id { get; set; }
        public virtual UserModel Кто_создал { get; set; }
    }
}