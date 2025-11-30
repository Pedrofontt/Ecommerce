using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EcommerceSystem.Data;
using EcommerceSystem.Models.Entities;
using EcommerceSystem.Services.Interfaces;

namespace EcommerceSystem.Controllers.Admin
{
    [Authorize(Roles = "Admin")]
    // ❌ ELIMINADO: [Area("Admin")] - Para que funcione como DashboardController
    public class CategoriasController : Controller
    {
        private readonly ICategoriaService _categoriaService;
        private readonly ApplicationDbContext _context;

        public CategoriasController(
            ICategoriaService categoriaService,
            ApplicationDbContext context)
        {
            _categoriaService = categoriaService;
            _context = context;
        }

        // GET: Categorias (ruta: /Categorias)
        public async Task<IActionResult> Index()
        {
            var categorias = await _context.Categorias
                .Include(c => c.Parent)
                .Include(c => c.Subcategorias)
                .Where(c => c.Activo)
                .OrderBy(c => c.Orden)
                .ToListAsync();

            // ✅ Especificar ruta completa de vista
            return View("~/Views/Admin/Categorias/Index.cshtml", categorias);
        }

        // GET: Categorias/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();

            var categoria = await _categoriaService.ObtenerPorIdAsync(id.Value);
            if (categoria == null)
                return NotFound();

            return View("~/Views/Admin/Categorias/Details.cshtml", categoria);
        }

        // GET: Categorias/Create
        public IActionResult Create()
        {
            ViewBag.Categorias = _context.Categorias
                .Where(c => c.Activo && c.ParentId == null)
                .OrderBy(c => c.Nombre)
                .ToList();

            return View("~/Views/Admin/Categorias/Create.cshtml");
        }

        // POST: Categorias/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Categoria categoria)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Categorias = _context.Categorias
                    .Where(c => c.Activo && c.ParentId == null)
                    .OrderBy(c => c.Nombre)
                    .ToList();
                return View("~/Views/Admin/Categorias/Create.cshtml", categoria);
            }

            var resultado = await _categoriaService.CrearAsync(categoria);
            if (resultado)
            {
                TempData["Success"] = "Categoría creada exitosamente";
                return RedirectToAction(nameof(Index));
            }

            TempData["Error"] = "Error al crear la categoría";
            return View("~/Views/Admin/Categorias/Create.cshtml", categoria);
        }

        // GET: Categorias/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();

            var categoria = await _categoriaService.ObtenerPorIdAsync(id.Value);
            if (categoria == null)
                return NotFound();

            ViewBag.Categorias = _context.Categorias
                .Where(c => c.Activo && c.ParentId == null && c.Id != id)
                .OrderBy(c => c.Nombre)
                .ToList();

            return View("~/Views/Admin/Categorias/Edit.cshtml", categoria);
        }

        // POST: Categorias/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Categoria categoria)
        {
            if (id != categoria.Id)
                return NotFound();

            if (!ModelState.IsValid)
            {
                ViewBag.Categorias = _context.Categorias
                    .Where(c => c.Activo && c.ParentId == null && c.Id != id)
                    .OrderBy(c => c.Nombre)
                    .ToList();
                return View("~/Views/Admin/Categorias/Edit.cshtml", categoria);
            }

            var resultado = await _categoriaService.ActualizarAsync(categoria);
            if (resultado)
            {
                TempData["Success"] = "Categoría actualizada exitosamente";
                return RedirectToAction(nameof(Index));
            }

            TempData["Error"] = "Error al actualizar la categoría";
            return View("~/Views/Admin/Categorias/Edit.cshtml", categoria);
        }

        // GET: Categorias/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
                return NotFound();

            var categoria = await _categoriaService.ObtenerPorIdAsync(id.Value);
            if (categoria == null)
                return NotFound();

            return View("~/Views/Admin/Categorias/Delete.cshtml", categoria);
        }

        // POST: Categorias/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var resultado = await _categoriaService.EliminarAsync(id);
            if (resultado)
            {
                TempData["Success"] = "Categoría eliminada exitosamente";
            }
            else
            {
                TempData["Error"] = "Error al eliminar la categoría";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}