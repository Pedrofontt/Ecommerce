using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using EcommerceSystem.Data;
using EcommerceSystem.Models.Entities;
using EcommerceSystem.Services.Interfaces;

namespace EcommerceSystem.Controllers.Admin
{
    [Authorize(Roles = "Admin,Vendedor")]
    [Area("Admin")]
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

        // GET: Admin/Ordenes
        public async Task<IActionResult> Index(string estado, DateTime? fechaInicio, DateTime? fechaFin)
        {
            var ordenes = _context.Ordenes
                .Include(o => o.Cliente)
                .AsQueryable();

            // Filtros
            if (!string.IsNullOrEmpty(estado))
            {
                ordenes = ordenes.Where(o => o.Estado == estado);
                ViewData["EstadoFiltro"] = estado;
            }

            if (fechaInicio.HasValue)
            {
                ordenes = ordenes.Where(o => o.FechaOrden >= fechaInicio.Value);
                ViewData["FechaInicio"] = fechaInicio.Value.ToString("yyyy-MM-dd");
            }

            if (fechaFin.HasValue)
            {
                ordenes = ordenes.Where(o => o.FechaOrden <= fechaFin.Value);
                ViewData["FechaFin"] = fechaFin.Value.ToString("yyyy-MM-dd");
            }

            var resultado = await ordenes
                .OrderByDescending(o => o.FechaOrden)
                .ToListAsync();

            // Estados para filtro
            ViewBag.Estados = new List<SelectListItem>
            {
                new SelectListItem { Value = "", Text = "Todos" },
                new SelectListItem { Value = "Pendiente", Text = "Pendiente" },
                new SelectListItem { Value = "Confirmado", Text = "Confirmado" },
                new SelectListItem { Value = "Enviado", Text = "Enviado" },
                new SelectListItem { Value = "Entregado", Text = "Entregado" },
                new SelectListItem { Value = "Cancelado", Text = "Cancelado" }
            };

            return View(resultado);
        }

        // GET: Admin/Ordenes/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();

            var orden = await _ordenService.ObtenerPorIdAsync(id.Value);
            if (orden == null)
                return NotFound();

            return View(orden);
        }

        // POST: Admin/Ordenes/CambiarEstado
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CambiarEstado(int id, string nuevoEstado)
        {
            var resultado = await _ordenService.CambiarEstadoAsync(id, nuevoEstado);

            if (resultado)
            {
                TempData["Success"] = $"Estado cambiado a {nuevoEstado} exitosamente";
            }
            else
            {
                TempData["Error"] = "No se pudo cambiar el estado de la orden";
            }

            return RedirectToAction(nameof(Details), new { id });
        }

        // POST: Admin/Ordenes/Cancelar
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Cancelar(int id)
        {
            var resultado = await _ordenService.CancelarOrdenAsync(id);

            if (resultado)
            {
                TempData["Success"] = "Orden cancelada exitosamente. El stock ha sido devuelto.";
            }
            else
            {
                TempData["Error"] = "No se pudo cancelar la orden";
            }

            return RedirectToAction(nameof(Details), new { id });
        }
    }
}