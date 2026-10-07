using System.ComponentModel.DataAnnotations;

namespace StockManagement.DTOs.Auth
{
    public class LoginRequestDto
    {
        [Required(ErrorMessage = "Kullanıcı adı zorunludur.")]
        [MaxLength(
            50,
            ErrorMessage = "Kullanıcı adı en fazla 50 karakter olabilir.")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Şifre zorunludur.")]
        [MaxLength(
            100,
            ErrorMessage = "Şifre en fazla 100 karakter olabilir.")]
        public string Password { get; set; } = string.Empty;
    }
}