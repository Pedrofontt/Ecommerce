using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EcommerceSystem.Models.Entities
{
    public class Orden
    {
        public int Id { get; set; }

        [Required, MaxLength(50)]
        public string NumeroOrden { get; set; }

        [Required]
        public int ClienteId { get; set; }

        public DateTime FechaOrden { get; set; } = DateTime.Now;

        [Required, MaxLength(20)]
        public string Estado { get; set; } = "Pendiente";

        [Column(TypeName = "decimal(18,2)")]
        public decimal Subtotal { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Descuento { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal Impuesto { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal CostoEnvio { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal Total { get; set; }

        [MaxLength(300)]
        public string? DireccionEnvio { get; set; }

        [MaxLength(500)]
        public string? NotasCliente { get; set; }

        [MaxLength(500)]
        public string? NotasInternas { get; set; }

        public DateTime? FechaPago { get; set; }
        public DateTime? FechaEnvio { get; set; }
        public DateTime? FechaEntrega { get; set; }

        [MaxLength(100)]
        public string? NumeroSeguimiento { get; set; }

        // Navegación
        public virtual Cliente Cliente { get; set; }
        public virtual ICollection<OrdenDetalle> Detalles { get; set; } = new List<OrdenDetalle>();
        public virtual ICollection<Pago> Pagos { get; set; } = new List<Pago>();
    }
}