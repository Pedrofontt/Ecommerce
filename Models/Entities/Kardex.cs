using System.ComponentModel.DataAnnotations;

namespace EcommerceSystem.Models.Entities
{
    public class Kardex
    {
        public int Id { get; set; }

        [Required]
        public int ProductoId { get; set; }

        [Required, MaxLength(30)]
        public string TipoMovimiento { get; set; }

        [Required]
        public int Cantidad { get; set; }

        [Required]
        public int StockAnterior { get; set; }

        [Required]
        public int StockNuevo { get; set; }

        public DateTime Fecha { get; set; } = DateTime.Now;

        [MaxLength(100)]
        public string? Referencia { get; set; }

        [MaxLength(300)]
        public string? Descripcion { get; set; }

        [MaxLength(450)]
        public string? UsuarioId { get; set; }

        // Navegación
        public virtual Producto Producto { get; set; }
    }
}
