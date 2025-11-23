using System.ComponentModel.DataAnnotations;

namespace EcommerceSystem.Models.Entities
{
    public class Carrito
    {
        public int Id { get; set; }

        [MaxLength(450)]
        public string? UsuarioId { get; set; }

        [MaxLength(100)]
        public string? SessionId { get; set; }

        public DateTime FechaCreacion { get; set; } = DateTime.Now;
        public DateTime FechaModificacion { get; set; } = DateTime.Now;

        // Navegación
        public virtual ICollection<CarritoItem> Items { get; set; } = new List<CarritoItem>();
    }
}