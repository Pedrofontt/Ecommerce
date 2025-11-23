namespace EcommerceSystem.Models.ViewModels
{
    public class DashboardViewModel
    {
        public decimal VentasHoy { get; set; }
        public decimal VentasSemana { get; set; }
        public decimal VentasMes { get; set; }

        public int OrdenesHoy { get; set; }
        public int OrdenesPendientes { get; set; }
        public int ProductosStockBajo { get; set; }

        public List<ProductoMasVendidoDto>? ProductosMasVendidos { get; set; }
        public List<VentaPorDiaDto>? VentasPorDia { get; set; }
        public List<AlertaStockDto>? AlertasRecientes { get; set; }
    }

    public class ProductoMasVendidoDto
    {
        public string Nombre { get; set; }
        public int CantidadVendida { get; set; }
        public decimal TotalVentas { get; set; }
    }

    public class VentaPorDiaDto
    {
        public DateTime Fecha { get; set; }
        public decimal Total { get; set; }
        public int NumeroOrdenes { get; set; }
    }

    public class AlertaStockDto
    {
        public string ProductoNombre { get; set; }
        public int StockActual { get; set; }
        public int StockMinimo { get; set; }
        public string Mensaje { get; set; }
    }
}