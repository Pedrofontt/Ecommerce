using System.ComponentModel.DataAnnotations;

namespace EcommerceSystem.Models.Entities
{
    public class ImagenProducto
    {
        public int Id { get; set; }

        [Required]
        public int ProductoId { get; set; }

        [Required, MaxLength(255)]
        public string RutaImagen { get; set; }

        public int Orden { get; set; } = 0;
        public bool EsPrincipal { get; set; } = false;

        // Navegación
        public virtual Producto Producto { get; set; }
    }
}