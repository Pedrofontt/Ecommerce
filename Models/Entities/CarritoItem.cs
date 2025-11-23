using System.ComponentModel.DataAnnotations;

namespace EcommerceSystem.Models.Entities
{
    public class CarritoItem
    {
        public int Id { get; set; }

        [Required]
        public int CarritoId { get; set; }

        [Required]
        public int ProductoId { get; set; }

        [Required]
        public int Cantidad { get; set; }

        public DateTime FechaAgregado { get; set; } = DateTime.Now;

        // Navegación
        public virtual Carrito Carrito { get; set; }
        public virtual Producto Producto { get; set; }
    }
}
