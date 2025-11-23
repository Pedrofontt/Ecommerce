using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EcommerceSystem.Services.Interfaces;
using System.Security.Claims;

namespace EcommerceSystem.Controllers.Admin
{
    [Authorize(Roles = "Admin,Vendedor")]
    [Area("Admin")]
    public class AlertasController : Controller
    {
        private readonly IAlertaService _alertaService;

        public AlertasController(IAlertaService alertaService)
        {
            _alertaService = alertaService;
        }

        // GET: Admin/Alertas
        public async Task<IActionResult> Index()
        {
            var alertas = await _alertaService.ObtenerAlertasPendientesAsync();
            return View(alertas);
        }

        // POST: Admin/Alertas/MarcarRevisada
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarcarRevisada(int id)
        {
            var usuarioId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var resultado = await _alertaService.MarcarComoRevisadaAsync(id, usuarioId);

            if (resultado)
            {
                TempData["Success"] = "Alerta marcada como revisada";
            }
            else
            {
                TempData["Error"] = "Error al marcar la alerta";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}