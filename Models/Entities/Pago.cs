using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EcommerceSystem.Models.Entities
{
    public class Pago
    {
        public int Id { get; set; }

        [Required]
        public int OrdenId { get; set; }

        [Required, MaxLength(50)]
        public string MetodoPago { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Monto { get; set; }

        public DateTime FechaPago { get; set; } = DateTime.Now;

        [Required, MaxLength(20)]
        public string Estado { get; set; } = "Pendiente";

        [MaxLength(100)]
        public string? TransaccionId { get; set; }

        [MaxLength(500)]
        public string? DetallesAdicionales { get; set; }

        // Navegación
        public virtual Orden Orden { get; set; }
    }
}
