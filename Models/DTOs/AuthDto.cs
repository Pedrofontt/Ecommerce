using System.ComponentModel.DataAnnotations;

namespace EcommerceSystem.Models.DTOs
{
    public class LoginDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        public string Password { get; set; }
    }

    public class RegisterDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [MinLength(8)]
        public string Password { get; set; }

        [Required]
        public string NombreCompleto { get; set; }

        public string? Telefono { get; set; }
    }

    public class AuthResponseDto
    {
        public string Token { get; set; }
        public string Email { get; set; }
        public string Rol { get; set; }
        public DateTime Expiracion { get; set; }
    }
}