using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MenuStolovaya.Models
{
    public class UserModel
    {
        [Key]
        public int Id { get; set; }
        public string Логин { get; set; }
        public string Пароль { get; set; }
        public string Фамилия { get; set; }
        public string Имя { get; set; }
        public string Отчество { get; set; }

        [ForeignKey("Роль")]
        public int Роль_id { get; set; }
        public virtual RoleModel Роль { get; set; }

        public bool Блокировка { get; set; }
        public DateTime Дата_регистрации { get; set; }

        // Вычисляемое свойство для полного имени
        public string ПолноеИмя => $"{Фамилия} {Имя} {Отчество}".Trim();
    }
}