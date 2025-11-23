using System.ComponentModel.DataAnnotations;

namespace EcommerceSystem.Models.Entities
{
    public class Proveedor
    {
        public int Id { get; set; }

        [Required, MaxLength(150)]
        public string Nombre { get; set; }

        [MaxLength(100)]
        public string? Contacto { get; set; }

        [MaxLength(100), EmailAddress]
        public string? Email { get; set; }

        [MaxLength(20)]
        public string? Telefono { get; set; }

        [MaxLength(200)]
        public string? Direccion { get; set; }

        public bool Activo { get; set; } = true;

        // Navegación
        public virtual ICollection<Producto> Productos { get; set; } = new List<Producto>();
    }
}