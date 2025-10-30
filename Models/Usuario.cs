using System.ComponentModel.DataAnnotations;

namespace Pension65Api.Models
{
    public class Usuario
    {
        [Key]
        public int Id { get; set; }

        [Required, StringLength(100)]
        public string Username { get; set; } = string.Empty;

        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        [StringLength(150)]
        public string? NombreCompleto { get; set; }

        [StringLength(50)]
        public string Role { get; set; } = "Admin";

        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    }
}
