using System.ComponentModel.DataAnnotations;

namespace Pension65Api.Models
{
    public class Beneficiario
    {
        public int Id { get; set; }

        [Required]
        [StringLength(12)]
        public required string DNI { get; set; }

        [Required]
        [StringLength(150)]
        public required string Nombres { get; set; }

        public int? Edad { get; set; }

        [StringLength(250)]
        public required string Direccion { get; set; }

        [StringLength(100)]
        public required string Distrito { get; set; }

        [StringLength(30)]
        public required string EstadoPago { get; set; } // ejemplo: "PAGADO", "PENDIENTE"
        
        public DateTime FechaRegistro { get; set; } = DateTime.UtcNow;
    }
}
