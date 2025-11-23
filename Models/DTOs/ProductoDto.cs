namespace EcommerceSystem.Models.DTOs
{
    public class ProductoDto
    {
        public int Id { get; set; }
        public string SKU { get; set; }
        public string Nombre { get; set; }
        public string? DescripcionCorta { get; set; }
        public decimal Precio { get; set; }
        public decimal? PrecioComparacion { get; set; }
        public int Stock { get; set; }
        public string? ImagenPrincipal { get; set; }
        public bool Destacado { get; set; }
        public string? CategoriaNombre { get; set; }
        public string? MarcaNombre { get; set; }
    }

    public class ProductoDetalleDto : ProductoDto
    {
        public string? Descripcion { get; set; }
        public decimal? Peso { get; set; }
        public string? Dimensiones { get; set; }
        public List<string>? Imagenes { get; set; }
    }

    public class CrearProductoDto
    {
        public string Nombre { get; set; }
        public string? Descripcion { get; set; }
        public decimal Precio { get; set; }
        public int Stock { get; set; }
        public int? CategoriaId { get; set; }
        public int? MarcaId { get; set; }
    }
}