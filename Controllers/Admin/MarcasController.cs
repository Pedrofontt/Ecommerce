using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EcommerceSystem.Data;
using EcommerceSystem.Models.Entities;

namespace EcommerceSystem.Controllers.Admin
{
    [Authorize(Roles = "Admin")]
    [Area("Admin")]
    public class MarcasController : Controller
    {
        private readonly ApplicationDbContext _context;

        public MarcasController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Admin/Marcas
        public async Task<IActionResult> Index()
        {
            var marcas = await _context.Marcas
                .Where(m => m.Activo)
                .OrderBy(m => m.Nombre)
                .ToListAsync();

            return View(marcas);
        }

        // GET: Admin/Marcas/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();

            var marca = await _context.Marcas
                .FirstOrDefaultAsync(m => m.Id == id);

            if (marca == null)
                return NotFound();

            return View(marca);
        }

        // GET: Admin/Marcas/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Admin/Marcas/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Marca marca)
        {
            if (!ModelState.IsValid)
                return View(marca);

            _context.Add(marca);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Marca creada exitosamente";
            return RedirectToAction(nameof(Index));
        }

        // GET: Admin/Marcas/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();

            var marca = await _context.Marcas.FindAsync(id);
            if (marca == null)
                return NotFound();

            return View(marca);
        }

        // POST: Admin/Marcas/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Marca marca)
        {
            if (id != marca.Id)
                return NotFound();

            if (!ModelState.IsValid)
                return View(marca);

            try
            {
                _context.Update(marca);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Marca actualizada exitosamente";
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!MarcaExists(marca.Id))
                    return NotFound();
                throw;
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: Admin/Marcas/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
                return NotFound();

            var marca = await _context.Marcas
                .FirstOrDefaultAsync(m => m.Id == id);

            if (marca == null)
                return NotFound();

            return View(marca);
        }

        // POST: Admin/Marcas/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var marca = await _context.Marcas.FindAsync(id);
            if (marca != null)
            {
                marca.Activo = false;
                await _context.SaveChangesAsync();
                TempData["Success"] = "Marca eliminada exitosamente";
            }

            return RedirectToAction(nameof(Index));
        }

        private bool MarcaExists(int id)
        {
            return _context.Marcas.Any(e => e.Id == id);
        }
    }
}