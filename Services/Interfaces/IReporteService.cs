using EcommerceSystem.Models.ViewModels;

namespace EcommerceSystem.Services.Interfaces
{
    public interface IReporteService
    {
        Task<DashboardViewModel> ObtenerDashboardAsync();
        Task<List<ProductoMasVendidoDto>> ObtenerProductosMasVendidosAsync(int top = 10);
        Task<List<VentaPorDiaDto>> ObtenerVentasPorPeriodoAsync(DateTime inicio, DateTime fin);
    }
}