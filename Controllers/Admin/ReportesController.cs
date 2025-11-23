using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EcommerceSystem.Data;
using EcommerceSystem.Services.Interfaces;

namespace EcommerceSystem.Controllers.Admin
{
    [Authorize(Roles = "Admin")]
    [Area("Admin")]
    public class ReportesController : Controller
    {
        private readonly IReporteService _reporteService;
        private readonly IKardexService _kardexService;
        private readonly ApplicationDbContext _context;

        public ReportesController(
            IReporteService reporteService,
            IKardexService kardexService,
            ApplicationDbContext context)
        {
            _reporteService = reporteService;
            _kardexService = kardexService;
            _context = context;
        }

        // GET: Admin/Reportes
        public IActionResult Index()
        {
            return View();
        }

        // GET: Admin/Reportes/Ventas
        public async Task<IActionResult> Ventas(DateTime? fechaInicio, DateTime? fechaFin)
        {
            fechaInicio ??= DateTime.Today.AddMonths(-1);
            fechaFin ??= DateTime.Today;

            ViewData["FechaInicio"] = fechaInicio.Value.ToString("yyyy-MM-dd");
            ViewData["FechaFin"] = fechaFin.Value.ToString("yyyy-MM-dd");

            var ventas = await _reporteService.ObtenerVentasPorPeriodoAsync(
                fechaInicio.Value, fechaFin.Value);

            return View(ventas);
        }

        // GET: Admin/Reportes/ProductosMasVendidos
        public async Task<IActionResult> ProductosMasVendidos(int top = 20)
        {
            ViewData["Top"] = top;
            var productos = await _reporteService.ObtenerProductosMasVendidosAsync(top);
            return View(productos);
        }

        // GET: Admin/Reportes/Inventario
        public async Task<IActionResult> Inventario()
        {
            var productos = await _context.Productos
                .Include(p => p.Categoria)
                .Include(p => p.Marca)
                .Where(p => p.Activo)
                .OrderBy(p => p.Nombre)
                .ToListAsync();

            return View(productos);
        }

        // GET: Admin/Reportes/StockBajo
        public async Task<IActionResult> StockBajo()
        {
            var productos = await _context.Productos
                .Include(p => p.Categoria)
                .Include(p => p.Marca)
                .Where(p => p.Activo && p.Stock <= p.StockMinimo)
                .OrderBy(p => p.Stock)
                .ToListAsync();

            return View(productos);
        }

        // GET: Admin/Reportes/Kardex
        public async Task<IActionResult> Kardex(int? productoId, DateTime? fechaInicio, DateTime? fechaFin)
        {
            ViewBag.Productos = await _context.Productos
                .Where(p => p.Activo)
                .OrderBy(p => p.Nombre)
                .ToListAsync();

            if (productoId.HasValue)
            {
                var movimientos = await _kardexService.ObtenerPorProductoAsync(productoId.Value);
                ViewData["ProductoId"] = productoId.Value;
                return View(movimientos);
            }
            else if (fechaInicio.HasValue && fechaFin.HasValue)
            {
                var movimientos = await _kardexService.ObtenerPorFechaAsync(
                    fechaInicio.Value, fechaFin.Value);
                ViewData["FechaInicio"] = fechaInicio.Value.ToString("yyyy-MM-dd");
                ViewData["FechaFin"] = fechaFin.Value.ToString("yyyy-MM-dd");
                return View(movimientos);
            }

            return View(new List<Models.Entities.Kardex>());
        }
    }
}