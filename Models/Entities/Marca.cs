using System.ComponentModel.DataAnnotations;

namespace EcommerceSystem.Models.Entities
{
    public class Marca
    {
        public int Id { get; set; }

        [Required, MaxLength(100)]
        public string Nombre { get; set; }

        [MaxLength(300)]
        public string? Descripcion { get; set; }

        [MaxLength(255)]
        public string? Logo { get; set; }

        public bool Activo { get; set; } = true;

        // Navegación
        public virtual ICollection<Producto> Productos { get; set; } = new List<Producto>();
    }
}