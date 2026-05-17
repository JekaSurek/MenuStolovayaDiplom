using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MenuStolovaya.Models
{
    public class ProductModel
    {
        [Key]
        public int Id { get; set; }
        public string Артикул { get; set; }
        public string Наименование { get; set; }

        [ForeignKey("Категория")]
        public int? Категория_id { get; set; }
        public virtual CategoryModel Категория { get; set; }

        public string Единица_измерения { get; set; }
        public decimal Потери_холодной_обработки { get; set; }
        public decimal Потери_горячей_обработки { get; set; }
        public decimal? Белки { get; set; }
        public decimal? Жиры { get; set; }
        public decimal? Углеводы { get; set; }
        public decimal? Калорийность { get; set; }
        public decimal Цена { get; set; }
        public bool Утверждена_цена { get; set; }

        [ForeignKey("КтоУтвердилЦену")]
        public int? Кто_утвердил_цену_id { get; set; }
        public virtual UserModel КтоУтвердилЦену { get; set; }

        public DateTime? Дата_утверждения_цены { get; set; }
        public bool Активен { get; set; }
    }
}