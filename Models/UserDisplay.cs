using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MenuStolovaya.Models
{
    public class UserDisplay
    {
        public int Id { get; set; }
        public string Логин { get; set; }
        public string ФИО { get; set; }
        public string Роль { get; set; }
        public bool Блокировка { get; set; }
        public string Дата_регистрации { get; set; }

        public string Фамилия { get; set; }
        public string Имя { get; set; }
        public string Отчество { get; set; }

        public string Пароль { get; set; }
    }
}