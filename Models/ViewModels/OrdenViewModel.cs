using System.ComponentModel.DataAnnotations;

namespace EcommerceSystem.Models.ViewModels
{
    public class OrdenViewModel
    {
        public int Id { get; set; }
        public string NumeroOrden { get; set; }

        [Required]
        [Display(Name = "Cliente")]
        public int ClienteId { get; set; }

        public string? NombreCliente { get; set; }
        public string? EmailCliente { get; set; }

        [Display(Name = "Fecha de Orden")]
        public DateTime FechaOrden { get; set; }

        [Required]
        [Display(Name = "Estado")]
        public string Estado { get; set; }

        [Display(Name = "Dirección de Envío")]
        [MaxLength(300)]
        public string? DireccionEnvio { get; set; }

        [Display(Name = "Notas del Cliente")]
        [MaxLength(500)]
        public string? NotasCliente { get; set; }

        [Display(Name = "Subtotal")]
        [DisplayFormat(DataFormatString = "{0:C2}")]
        public decimal Subtotal { get; set; }

        [Display(Name = "Descuento")]
        [DisplayFormat(DataFormatString = "{0:C2}")]
        public decimal Descuento { get; set; }

        [Display(Name = "Impuesto")]
        [DisplayFormat(DataFormatString = "{0:C2}")]
        public decimal Impuesto { get; set; }

        [Display(Name = "Costo de Envío")]
        [DisplayFormat(DataFormatString = "{0:C2}")]
        public decimal CostoEnvio { get; set; }

        [Display(Name = "Total")]
        [DisplayFormat(DataFormatString = "{0:C2}")]
        public decimal Total { get; set; }

        public List<OrdenDetalleViewModel>? Detalles { get; set; }
    }

    public class OrdenDetalleViewModel
    {
        public int ProductoId { get; set; }
        public string NombreProducto { get; set; }
        public int Cantidad { get; set; }
        public decimal PrecioUnitario { get; set; }
        public decimal Subtotal { get; set; }
    }
}