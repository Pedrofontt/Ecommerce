using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EcommerceSystem.Models.Entities
{
    public class Producto
    {
        public int Id { get; set; }

        [Required, MaxLength(50)]
        public string SKU { get; set; }

        [Required, MaxLength(150)]
        public string Nombre { get; set; }

        public string? Descripcion { get; set; }

        [MaxLength(500)]
        public string? DescripcionCorta { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Precio { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? PrecioComparacion { get; set; }

        [Required]
        public int Stock { get; set; } = 0;

        public int StockMinimo { get; set; } = 5;

        public int? CategoriaId { get; set; }
        public int? MarcaId { get; set; }
        public int? ProveedorId { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal? Peso { get; set; }

        [MaxLength(50)]
        public string? Dimensiones { get; set; }

        [MaxLength(255)]
        public string? ImagenPrincipal { get; set; }

        public bool Destacado { get; set; } = false;
        public bool Activo { get; set; } = true;

        public DateTime FechaCreacion { get; set; } = DateTime.Now;
        public DateTime? FechaModificacion { get; set; }

        // Navegación
        public virtual Categoria? Categoria { get; set; }
        public virtual Marca? Marca { get; set; }
        public virtual Proveedor? Proveedor { get; set; }
        public virtual ICollection<ProductoCategoria> ProductoCategorias { get; set; } = new List<ProductoCategoria>();
        public virtual ICollection<ImagenProducto> Imagenes { get; set; } = new List<ImagenProducto>();
        public virtual ICollection<Kardex> MovimientosKardex { get; set; } = new List<Kardex>();
    }
}