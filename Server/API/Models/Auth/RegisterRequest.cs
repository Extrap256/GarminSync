using System.ComponentModel.DataAnnotations;
using static Server.Common.Settings.Constants;

namespace Server.API.Models.Auth
{
    public class RegisterRequest
    {
        [Required(ErrorMessage = "Логин обязателен")]
        [MinLength(LoginMinLength, ErrorMessage = "Для логина нужно больше символов")]
        [MaxLength(LoginMaxLength, ErrorMessage = "Для логина нужно меньше символов")]
        public string Login { get; set; } = string.Empty;

        [Required(ErrorMessage = "Пароль обязателен")]
        [MinLength(PasswordMinLength, ErrorMessage = "Для пароля нужно больше символов")]
        [RegularExpression(@"^[A-Za-z0-9!@#$%^&*()_+\-=\[\]{};':"",.<>?/\\|`~]+$",
        ErrorMessage = "Пароль содержит недопустимые символы")]
        public string Password { get; set; } = string.Empty;

        [Required]
        [MaxLength(NameMaxLength)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [MaxLength(NameMaxLength)]
        public string LastName {  get; set; } = string.Empty;

        public ushort? YearOfBirth {  get; set; }
    }
}
