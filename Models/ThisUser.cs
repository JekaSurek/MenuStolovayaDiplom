using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MenuStolovaya.Models
{
    public class ThisUser
    {
        // Статическое свойство для хранения текущего пользователя
        public static ThisUser CurrentUser { get; private set; }

        public int Id { get; set; }
        public string Login { get; set; }
        public string Password { get; set; }
        public string Surname { get; set; }
        public string FirstName { get; set; }
        public string MiddleName { get; set; }
        public string FullName { get; set; }
        public int RoleId { get; set; }
        public string RoleName { get; set; }
        public bool IsBlocked { get; set; }

        public static void SetCurrentUser(Пользователи user, string roleName)
        {
            if (user != null)
            {
                CurrentUser = new ThisUser
                {
                    Id = user.id,
                    Login = user.Логин,
                    Password = user.Пароль,
                    Surname = user.Фамилия,
                    FirstName = user.Имя,
                    MiddleName = user.Отчество ?? "",
                    FullName = $"{user.Фамилия} {user.Имя} {user.Отчество}".Trim(),
                    RoleId = user.Роль_id,
                    RoleName = roleName,
                    IsBlocked = user.Блокировка ?? false // Исправление для nullable bool
                };
            }
        }

        public static void ClearCurrentUser()
        {
            CurrentUser = null;
        }

        public static bool IsAdmin => CurrentUser?.RoleName == "Администратор";
        public static bool IsTechnologist => CurrentUser?.RoleName == "Технолог";
        public static bool IsAccountant => CurrentUser?.RoleName == "Бухгалтер";
    }
}   