using Microsoft.EntityFrameworkCore;
using EcommerceSystem.Data;
using EcommerceSystem.Models.ViewModels;
using EcommerceSystem.Services.Interfaces;

namespace EcommerceSystem.Services.Implementations
{
    public class ReporteService : IReporteService
    {
        private readonly ApplicationDbContext _context;

        public ReporteService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<DashboardViewModel> ObtenerDashboardAsync()
        {
            var hoy = DateTime.Today;
            var inicioDeSemana = hoy.AddDays(-(int)hoy.DayOfWeek);
            var inicioDelMes = new DateTime(hoy.Year, hoy.Month, 1);

            var dashboard = new DashboardViewModel
            {
                VentasHoy = await _context.Ordenes
                    .Where(o => o.FechaOrden.Date == hoy && o.Estado != "Cancelado")
                    .SumAsync(o => o.Total),

                VentasSemana = await _context.Ordenes
                    .Where(o => o.FechaOrden >= inicioDeSemana && o.Estado != "Cancelado")
                    .SumAsync(o => o.Total),

                VentasMes = await _context.Ordenes
                    .Where(o => o.FechaOrden >= inicioDelMes && o.Estado != "Cancelado")
                    .SumAsync(o => o.Total),

                OrdenesHoy = await _context.Ordenes
                    .CountAsync(o => o.FechaOrden.Date == hoy),

                OrdenesPendientes = await _context.Ordenes
                    .CountAsync(o => o.Estado == "Pendiente"),

                ProductosStockBajo = await _context.Productos
                    .CountAsync(p => p.Stock <= p.StockMinimo && p.Activo)
            };

            dashboard.ProductosMasVendidos = await ObtenerProductosMasVendidosAsync(5);
            dashboard.VentasPorDia = await ObtenerVentasPorPeriodoAsync(hoy.AddDays(-7), hoy);

            return dashboard;
        }

        public async Task<List<ProductoMasVendidoDto>> ObtenerProductosMasVendidosAsync(int top = 10)
        {
            return await _context.OrdenDetalles
                .Include(od => od.Producto)
                .GroupBy(od => new { od.ProductoId, od.Producto.Nombre })
                .Select(g => new ProductoMasVendidoDto
                {
                    Nombre = g.Key.Nombre,
                    CantidadVendida = g.Sum(od => od.Cantidad),
                    TotalVentas = g.Sum(od => od.Subtotal)
                })
                .OrderByDescending(p => p.CantidadVendida)
                .Take(top)
                .ToListAsync();
        }

        public async Task<List<VentaPorDiaDto>> ObtenerVentasPorPeriodoAsync(DateTime inicio, DateTime fin)
        {
            return await _context.Ordenes
                .Where(o => o.FechaOrden >= inicio && o.FechaOrden <= fin && o.Estado != "Cancelado")
                .GroupBy(o => o.FechaOrden.Date)
                .Select(g => new VentaPorDiaDto
                {
                    Fecha = g.Key,
                    Total = g.Sum(o => o.Total),
                    NumeroOrdenes = g.Count()
                })
                .OrderBy(v => v.Fecha)
                .ToListAsync();
        }
    }
}