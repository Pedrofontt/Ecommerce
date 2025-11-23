using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace EcommerceSystem.Models.ViewModels
{
    public class ProductoViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "El nombre es requerido")]
        [MaxLength(150, ErrorMessage = "Máximo 150 caracteres")]
        [Display(Name = "Nombre del Producto")]
        public string Nombre { get; set; }

        [Display(Name = "Descripción")]
        public string? Descripcion { get; set; }

        [MaxLength(500)]
        [Display(Name = "Descripción Corta")]
        public string? DescripcionCorta { get; set; }

        [Required]
        [Range(0.01, 999999.99, ErrorMessage = "El precio debe ser mayor a 0")]
        [Display(Name = "Precio")]
        [DisplayFormat(DataFormatString = "{0:C2}")]
        public decimal Precio { get; set; }

        [Display(Name = "Precio Antes del Descuento")]
        [DisplayFormat(DataFormatString = "{0:C2}")]
        public decimal? PrecioComparacion { get; set; }

        [Required]
        [Range(0, int.MaxValue, ErrorMessage = "El stock no puede ser negativo")]
        [Display(Name = "Stock Disponible")]
        public int Stock { get; set; }

        [Display(Name = "Stock Mínimo")]
        public int StockMinimo { get; set; } = 5;

        [Display(Name = "Categoría Principal")]
        public int? CategoriaId { get; set; }

        [Display(Name = "Marca")]
        public int? MarcaId { get; set; }

        [Display(Name = "Proveedor")]
        public int? ProveedorId { get; set; }

        [Display(Name = "Imagen Principal")]
        public IFormFile? ImagenFile { get; set; }

        public string? ImagenActual { get; set; }

        [Display(Name = "Producto Destacado")]
        public bool Destacado { get; set; }

        [Display(Name = "Activo")]
        public bool Activo { get; set; } = true;

        // Para dropdowns
        public List<SelectListItem>? Categorias { get; set; }
        public List<SelectListItem>? Marcas { get; set; }
        public List<SelectListItem>? Proveedores { get; set; }
    }
}