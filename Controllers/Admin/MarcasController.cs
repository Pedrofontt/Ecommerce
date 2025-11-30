using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EcommerceSystem.Data;
using EcommerceSystem.Models.Entities;

namespace EcommerceSystem.Controllers.Admin
{
    [Authorize(Roles = "Admin")]
    public class MarcasController : Controller
    {
        private readonly ApplicationDbContext _context;

        public MarcasController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Marcas
        public async Task<IActionResult> Index()
        {
            var marcas = await _context.Marcas
                .Include(m => m.Productos)
                .OrderBy(m => m.Nombre)
                .ToListAsync();

            return View("~/Views/Admin/Marcas/Index.cshtml", marcas);
        }

        // GET: Marcas/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();

            var marca = await _context.Marcas
                .Include(m => m.Productos)
                    .ThenInclude(p => p.Categoria)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (marca == null)
                return NotFound();

            return View("~/Views/Admin/Marcas/Details.cshtml", marca);
        }

        // GET: Marcas/Create
        public IActionResult Create()
        {
            return View("~/Views/Admin/Marcas/Create.cshtml");
        }

        // POST: Marcas/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Marca marca)
        {
            if (!ModelState.IsValid)
            {
                return View("~/Views/Admin/Marcas/Create.cshtml", marca);
            }

            try
            {
                _context.Add(marca);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Marca creada exitosamente";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception)
            {
                TempData["Error"] = "Error al crear la marca";
                return View("~/Views/Admin/Marcas/Create.cshtml", marca);
            }
        }

        // GET: Marcas/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();

            var marca = await _context.Marcas
                .Include(m => m.Productos)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (marca == null)
                return NotFound();

            return View("~/Views/Admin/Marcas/Edit.cshtml", marca);
        }

        // POST: Marcas/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Marca marca)
        {
            if (id != marca.Id)
                return NotFound();

            if (!ModelState.IsValid)
            {
                return View("~/Views/Admin/Marcas/Edit.cshtml", marca);
            }

            try
            {
                _context.Update(marca);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Marca actualizada exitosamente";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!MarcaExists(marca.Id))
                    return NotFound();
                else
                    throw;
            }
            catch (Exception)
            {
                TempData["Error"] = "Error al actualizar la marca";
                return View("~/Views/Admin/Marcas/Edit.cshtml", marca);
            }
        }

        // GET: Marcas/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
                return NotFound();

            var marca = await _context.Marcas
                .Include(m => m.Productos)
                    .ThenInclude(p => p.Categoria)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (marca == null)
                return NotFound();

            return View("~/Views/Admin/Marcas/Delete.cshtml", marca);
        }

        // POST: Marcas/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                var marca = await _context.Marcas.FindAsync(id);
                if (marca == null)
                {
                    TempData["Error"] = "Marca no encontrada";
                    return RedirectToAction(nameof(Index));
                }

                // Soft delete
                marca.Activo = false;
                _context.Update(marca);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Marca eliminada exitosamente";
            }
            catch (Exception)
            {
                TempData["Error"] = "Error al eliminar la marca. Puede tener productos asociados.";
            }

            return RedirectToAction(nameof(Index));
        }

        private bool MarcaExists(int id)
        {
            return _context.Marcas.Any(e => e.Id == id);
        }
    }
}