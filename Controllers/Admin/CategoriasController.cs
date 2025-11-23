using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EcommerceSystem.Data;
using EcommerceSystem.Models.Entities;
using EcommerceSystem.Services.Interfaces;

namespace EcommerceSystem.Controllers.Admin
{
    [Authorize(Roles = "Admin")]
    [Area("Admin")]
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

        // GET: Admin/Categorias
        public async Task<IActionResult> Index()
        {
            var categorias = await _context.Categorias
                .Include(c => c.Parent)
                .Include(c => c.Subcategorias)
                .Where(c => c.Activo)
                .OrderBy(c => c.Orden)
                .ToListAsync();

            return View(categorias);
        }

        // GET: Admin/Categorias/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();

            var categoria = await _categoriaService.ObtenerPorIdAsync(id.Value);
            if (categoria == null)
                return NotFound();

            return View(categoria);
        }

        // GET: Admin/Categorias/Create
        public IActionResult Create()
        {
            ViewBag.Categorias = _context.Categorias
                .Where(c => c.Activo && c.ParentId == null)
                .OrderBy(c => c.Nombre)
                .ToList();

            return View();
        }

        // POST: Admin/Categorias/Create
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
                return View(categoria);
            }

            var resultado = await _categoriaService.CrearAsync(categoria);

            if (resultado)
            {
                TempData["Success"] = "Categoría creada exitosamente";
                return RedirectToAction(nameof(Index));
            }

            TempData["Error"] = "Error al crear la categoría";
            return View(categoria);
        }

        // GET: Admin/Categorias/Edit/5
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

            return View(categoria);
        }

        // POST: Admin/Categorias/Edit/5
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
                return View(categoria);
            }

            var resultado = await _categoriaService.ActualizarAsync(categoria);

            if (resultado)
            {
                TempData["Success"] = "Categoría actualizada exitosamente";
                return RedirectToAction(nameof(Index));
            }

            TempData["Error"] = "Error al actualizar la categoría";
            return View(categoria);
        }

        // GET: Admin/Categorias/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
                return NotFound();

            var categoria = await _categoriaService.ObtenerPorIdAsync(id.Value);
            if (categoria == null)
                return NotFound();

            return View(categoria);
        }

        // POST: Admin/Categorias/Delete/5
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