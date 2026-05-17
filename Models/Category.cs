using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace MenuStolovaya.Models
{
    public class CategoryModel
    {
        [Key]
        public int Id { get; set; }
        public string Наименование { get; set; }
        public string Описание { get; set; }
    }
}