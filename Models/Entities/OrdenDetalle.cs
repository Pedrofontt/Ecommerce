using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EcommerceSystem.Models.Entities
{
    public class OrdenDetalle
    {
        public int Id { get; set; }

        [Required]
        public int OrdenId { get; set; }

        [Required]
        public int ProductoId { get; set; }

        [Required]
        public int Cantidad { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal PrecioUnitario { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Descuento { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal Subtotal { get; set; }

        // Navegación
        public virtual Orden Orden { get; set; }
        public virtual Producto Producto { get; set; }
    }
}
