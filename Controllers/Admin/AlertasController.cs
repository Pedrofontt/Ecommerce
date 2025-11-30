using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EcommerceSystem.Data;
using EcommerceSystem.Models.Entities;
using EcommerceSystem.Services.Interfaces;

namespace EcommerceSystem.Controllers.Admin
{
    [Authorize(Roles = "Admin,Vendedor")]
    public class AlertasController : Controller
    {
        private readonly IAlertaService _alertaService;
        private readonly ApplicationDbContext _context;

        public AlertasController(
            IAlertaService alertaService,
            ApplicationDbContext context)
        {
            _alertaService = alertaService;
            _context = context;
        }

        // GET: Alertas
        public async Task<IActionResult> Index(string tipo, bool soloPendientes = true)
        {
            ViewData["TipoFiltro"] = tipo;
            ViewData["SoloPendientes"] = soloPendientes;

            var alertasQuery = _context.AlertasStock
                .Include(a => a.Producto)
                .AsQueryable();

            // Filtro por estado
            if (soloPendientes)
            {
                alertasQuery = alertasQuery.Where(a => a.Estado == "Pendiente");
            }

            // Filtro por tipo
            if (!string.IsNullOrEmpty(tipo) && tipo != "Todos")
            {
                alertasQuery = alertasQuery.Where(a => a.Tipo == tipo);
            }

            var alertas = await alertasQuery
                .OrderByDescending(a => a.Fecha)
                .ToListAsync();

            return View("~/Views/Admin/Alertas/Index.cshtml", alertas);
        }

        // GET: Alertas/GenerarAlertasManual
        public IActionResult GenerarAlertasManual()
        {
            return View("~/Views/Admin/Alertas/GenerarAlertasManual.cshtml");
        }

        // POST: Alertas/GenerarAlertasManual
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GenerarAlertasManualPost()
        {
            try
            {
                var productosStockBajo = await _context.Productos
                    .Where(p => p.Activo && p.Stock <= p.StockMinimo)
                    .ToListAsync();

                int alertasCreadas = 0;

                foreach (var producto in productosStockBajo)
                {
                    // Verificar si ya existe una alerta pendiente para este producto
                    var alertaExistente = await _context.AlertasStock
                        .FirstOrDefaultAsync(a => a.ProductoId == producto.Id && a.Estado == "Pendiente");

                    if (alertaExistente == null)
                    {
                        var tipo = "StockBajo";
                        var porcentajeStock = producto.StockMinimo > 0
                            ? (decimal)producto.Stock / producto.StockMinimo * 100
                            : 0;

                        if (producto.Stock == 0)
                            tipo = "Crítico";
                        else if (porcentajeStock <= 25)
                            tipo = "Crítico";

                        var alerta = new AlertaStock
                        {
                            ProductoId = producto.Id,
                            Tipo = tipo,
                            Mensaje = $"Stock bajo: {producto.Stock} unidades (Mínimo: {producto.StockMinimo})",
                            Fecha = DateTime.Now,
                            Estado = "Pendiente"
                        };

                        _context.AlertasStock.Add(alerta);
                        alertasCreadas++;
                    }
                }

                await _context.SaveChangesAsync();

                if (alertasCreadas > 0)
                {
                    TempData["Success"] = $"{alertasCreadas} nueva(s) alerta(s) generada(s)";
                }
                else
                {
                    TempData["Info"] = "No se encontraron productos con stock bajo sin alerta";
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error al generar alertas: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: Alertas/Resolver/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Resolver(int id)
        {
            try
            {
                var alerta = await _context.AlertasStock
                    .Include(a => a.Producto)
                    .FirstOrDefaultAsync(a => a.Id == id);

                if (alerta == null)
                {
                    TempData["Error"] = "Alerta no encontrada";
                    return RedirectToAction(nameof(Index));
                }

                // Verificar si el stock actual es suficiente
                if (alerta.Producto.Stock > alerta.Producto.StockMinimo)
                {
                    alerta.Estado = "Resuelta";
                    alerta.FechaRevision = DateTime.Now;
                    alerta.RevisadoPor = User.Identity?.Name;

                    _context.Update(alerta);
                    await _context.SaveChangesAsync();

                    TempData["Success"] = "Alerta resuelta exitosamente";
                }
                else
                {
                    TempData["Warning"] = "El stock actual sigue siendo bajo. Aumente el stock antes de resolver la alerta.";
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error al resolver alerta: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}