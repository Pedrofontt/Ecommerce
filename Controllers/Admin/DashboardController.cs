using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EcommerceSystem.Services.Interfaces;

namespace EcommerceSystem.Controllers.Admin
{
    [Authorize(Roles = "Admin,Vendedor")]
    [Area("Admin")]
    public class DashboardController : Controller
    {
        private readonly IReporteService _reporteService;
        private readonly IAlertaService _alertaService;

        public DashboardController(
            IReporteService reporteService,
            IAlertaService alertaService)
        {
            _reporteService = reporteService;
            _alertaService = alertaService;
        }

        // GET: Admin/Dashboard
        public async Task<IActionResult> Index()
        {
            var dashboard = await _reporteService.ObtenerDashboardAsync();
            dashboard.AlertasRecientes = await ObtenerAlertasAsync();

            return View(dashboard);
        }

        private async Task<List<Models.ViewModels.AlertaStockDto>> ObtenerAlertasAsync()
        {
            var alertas = await _alertaService.ObtenerAlertasPendientesAsync();

            return alertas.Select(a => new Models.ViewModels.AlertaStockDto
            {
                ProductoNombre = a.Producto.Nombre,
                StockActual = a.Producto.Stock,
                StockMinimo = a.Producto.StockMinimo,
                Mensaje = a.Mensaje
            }).Take(5).ToList();
        }
    }
}