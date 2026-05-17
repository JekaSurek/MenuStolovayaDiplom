using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MenuStolovaya.Models
{
    public class RecipeModel
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey("Технологическая_карта")]
        public int Технологическая_карта_id { get; set; }
        public virtual TechnologyCardModel Технологическая_карта { get; set; }

        [ForeignKey("Продукт")]
        public int Продукт_id { get; set; }
        public virtual ProductModel Продукт { get; set; }

        [Required]
        public decimal Количество_брутто { get; set; }

        public decimal? Количество_нетто { get; set; }
        public int Порядок_закладки { get; set; }
    }
}