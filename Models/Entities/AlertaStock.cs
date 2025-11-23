using System.ComponentModel.DataAnnotations;

namespace EcommerceSystem.Models.Entities
{
    public class AlertaStock
    {
        public int Id { get; set; }

        [Required]
        public int ProductoId { get; set; }

        [Required, MaxLength(300)]
        public string Mensaje { get; set; }

        [Required, MaxLength(20)]
        public string Tipo { get; set; } = "StockBajo";

        public DateTime Fecha { get; set; } = DateTime.Now;

        [Required, MaxLength(20)]
        public string Estado { get; set; } = "Pendiente";

        public DateTime? FechaRevision { get; set; }

        [MaxLength(450)]
        public string? RevisadoPor { get; set; }

        // Navegación
        public virtual Producto Producto { get; set; }
    }
}