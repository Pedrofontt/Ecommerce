using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EcommerceSystem.Data;
using EcommerceSystem.Models.Entities;
using EcommerceSystem.Services.Interfaces;

namespace EcommerceSystem.Controllers.Admin
{
    [Authorize(Roles = "Admin,Vendedor")]
    public class OrdenesController : Controller
    {
        private readonly IOrdenService _ordenService;
        private readonly ApplicationDbContext _context;

        public OrdenesController(
            IOrdenService ordenService,
            ApplicationDbContext context)
        {
            _ordenService = ordenService;
            _context = context;
        }

        // GET: Ordenes
        public async Task<IActionResult> Index(string estado, string busqueda, DateTime? fechaDesde, DateTime? fechaHasta)
        {
            ViewData["EstadoFiltro"] = estado;
            ViewData["BusquedaFiltro"] = busqueda;
            ViewData["FechaDesde"] = fechaDesde?.ToString("yyyy-MM-dd");
            ViewData["FechaHasta"] = fechaHasta?.ToString("yyyy-MM-dd");

            var ordenesQuery = _context.Ordenes
                .Include(o => o.Cliente)
                .Include(o => o.Detalles)
                    .ThenInclude(d => d.Producto)
                .AsQueryable();

            // Filtro por estado
            if (!string.IsNullOrEmpty(estado) && estado != "Todos")
            {
                ordenesQuery = ordenesQuery.Where(o => o.Estado == estado);
            }

            // Filtro por búsqueda (número de orden o cliente)
            if (!string.IsNullOrEmpty(busqueda))
            {
                ordenesQuery = ordenesQuery.Where(o =>
                    o.NumeroOrden.Contains(busqueda) ||
                    o.Cliente.NombreCompleto.Contains(busqueda) ||
                    o.Cliente.Email.Contains(busqueda)
                );
            }

            // Filtro por rango de fechas
            if (fechaDesde.HasValue)
            {
                ordenesQuery = ordenesQuery.Where(o => o.FechaOrden >= fechaDesde.Value);
            }

            if (fechaHasta.HasValue)
            {
                var fechaHastaFin = fechaHasta.Value.AddDays(1).AddTicks(-1);
                ordenesQuery = ordenesQuery.Where(o => o.FechaOrden <= fechaHastaFin);
            }

            var ordenes = await ordenesQuery
                .OrderByDescending(o => o.FechaOrden)
                .ToListAsync();

            return View("~/Views/Admin/Ordenes/Index.cshtml", ordenes);
        }

        // GET: Ordenes/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();

            var orden = await _context.Ordenes
                .Include(o => o.Cliente)
                .Include(o => o.Detalles)
                    .ThenInclude(d => d.Producto)
                .Include(o => o.Pagos)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (orden == null)
                return NotFound();

            return View("~/Views/Admin/Ordenes/Details.cshtml", orden);
        }

        // GET: Ordenes/CambiarEstado/5
        public async Task<IActionResult> CambiarEstado(int? id)
        {
            if (id == null)
                return NotFound();

            var orden = await _context.Ordenes
                .Include(o => o.Cliente)
                .Include(o => o.Detalles)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (orden == null)
                return NotFound();

            return View("~/Views/Admin/Ordenes/CambiarEstado.cshtml", orden);
        }

        // POST: Ordenes/ActualizarEstado/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ActualizarEstado(int id, string nuevoEstado)
        {
            try
            {
                var orden = await _context.Ordenes.FindAsync(id);
                if (orden == null)
                {
                    TempData["Error"] = "Orden no encontrada";
                    return RedirectToAction(nameof(Index));
                }

                // Validar transición de estado
                if (!ValidarTransicionEstado(orden.Estado, nuevoEstado))
                {
                    TempData["Error"] = $"No se puede cambiar de estado {orden.Estado} a {nuevoEstado}";
                    return RedirectToAction(nameof(Details), new { id });
                }

                orden.Estado = nuevoEstado;

                // Actualizar fecha según el estado
                switch (nuevoEstado)
                {
                    case "Pagado":
                        orden.FechaPago = DateTime.Now;
                        break;
                    case "Enviado":
                        orden.FechaEnvio = DateTime.Now;
                        break;
                    case "Entregado":
                        orden.FechaEntrega = DateTime.Now;
                        break;
                    case "Cancelado":
                        // Restaurar inventario si se cancela
                        await RestaurarInventarioAsync(id);
                        break;
                }

                _context.Update(orden);
                await _context.SaveChangesAsync();

                TempData["Success"] = $"Estado actualizado a '{nuevoEstado}' exitosamente";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error al actualizar estado: {ex.Message}";
            }

            return RedirectToAction(nameof(Details), new { id });
        }

        // POST: Ordenes/Cancelar/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancelar(int id)
        {
            try
            {
                var orden = await _context.Ordenes.FindAsync(id);
                if (orden == null)
                {
                    TempData["Error"] = "Orden no encontrada";
                    return RedirectToAction(nameof(Index));
                }

                if (orden.Estado == "Entregado" || orden.Estado == "Cancelado")
                {
                    TempData["Error"] = "No se puede cancelar una orden entregada o ya cancelada";
                    return RedirectToAction(nameof(Details), new { id });
                }

                orden.Estado = "Cancelado";

                // Agregar motivo a notas internas
                var notaCancelacion = $"Cancelado ({DateTime.Now:dd/MM/yyyy HH:mm})";
                orden.NotasInternas = string.IsNullOrEmpty(orden.NotasInternas)
                    ? notaCancelacion
                    : $"{orden.NotasInternas}\n{notaCancelacion}";

                // Restaurar inventario
                await RestaurarInventarioAsync(id);

                _context.Update(orden);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Orden cancelada exitosamente";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error al cancelar orden: {ex.Message}";
            }

            return RedirectToAction(nameof(Details), new { id });
        }

        // Métodos auxiliares
        private bool ValidarTransicionEstado(string estadoActual, string nuevoEstado)
        {
            // Definir transiciones válidas
            var transicionesValidas = new Dictionary<string, List<string>>
            {
                { "Pendiente", new List<string> { "Pagado", "Cancelado" } },
                { "Pagado", new List<string> { "Enviado", "Cancelado" } },
                { "Enviado", new List<string> { "Entregado", "Cancelado" } },
                { "Entregado", new List<string> { } }, // Estado final
                { "Cancelado", new List<string> { } }  // Estado final
            };

            return transicionesValidas.ContainsKey(estadoActual) &&
                   transicionesValidas[estadoActual].Contains(nuevoEstado);
        }

        private async Task RestaurarInventarioAsync(int ordenId)
        {
            var detalles = await _context.OrdenDetalles
                .Include(d => d.Producto)
                .Include(d => d.Orden)
                .Where(d => d.OrdenId == ordenId)
                .ToListAsync();

            foreach (var detalle in detalles)
            {
                var stockAnterior = detalle.Producto.Stock;
                detalle.Producto.Stock += detalle.Cantidad;
                _context.Update(detalle.Producto);

                // Registrar en kardex
                var kardex = new Kardex
                {
                    ProductoId = detalle.ProductoId,
                    TipoMovimiento = "Entrada",
                    Cantidad = detalle.Cantidad,
                    StockAnterior = stockAnterior,
                    StockNuevo = detalle.Producto.Stock,
                    Referencia = $"Orden #{detalle.Orden.NumeroOrden}",
                    Descripcion = $"Cancelación de orden",
                    Fecha = DateTime.Now
                };

                _context.Kardex.Add(kardex);
            }

            await _context.SaveChangesAsync();
        }

        // API para estadísticas rápidas
        [HttpGet]
        public async Task<IActionResult> ObtenerEstadisticas()
        {
            var stats = new
            {
                TotalPendientes = await _context.Ordenes.CountAsync(o => o.Estado == "Pendiente"),
                TotalPagadas = await _context.Ordenes.CountAsync(o => o.Estado == "Pagado"),
                TotalEnviadas = await _context.Ordenes.CountAsync(o => o.Estado == "Enviado"),
                TotalEntregadas = await _context.Ordenes.CountAsync(o => o.Estado == "Entregado"),
                TotalCanceladas = await _context.Ordenes.CountAsync(o => o.Estado == "Cancelado"),
                VentasHoy = await _context.Ordenes
                    .Where(o => o.FechaOrden.Date == DateTime.Today && o.Estado != "Cancelado")
                    .SumAsync(o => o.Total)
            };

            return Json(stats);
        }
    }
}