using System.ComponentModel.DataAnnotations;

namespace EcommerceSystem.Models.Entities
{
    public class Categoria
    {
        public int Id { get; set; }

        [Required, MaxLength(150)]
        public string Nombre { get; set; }

        [MaxLength(500)]
        public string? Descripcion { get; set; }

        public int? ParentId { get; set; }

        [MaxLength(50)]
        public string? Icono { get; set; }

        public int Orden { get; set; } = 0;

        public bool Activo { get; set; } = true;

        // Navegación
        public virtual Categoria? Parent { get; set; }
        public virtual ICollection<Categoria> Subcategorias { get; set; } = new List<Categoria>();
        public virtual ICollection<ProductoCategoria> ProductoCategorias { get; set; } = new List<ProductoCategoria>();
    }
}