using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EcommerceSystem.Data;
using EcommerceSystem.Models.Entities;

namespace EcommerceSystem.Controllers.Admin
{
    [Authorize(Roles = "Admin")]
    [Area("Admin")]
    public class ProveedoresController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ProveedoresController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Admin/Proveedores
        public async Task<IActionResult> Index()
        {
            var proveedores = await _context.Proveedores
                .Where(p => p.Activo)
                .OrderBy(p => p.Nombre)
                .ToListAsync();

            return View(proveedores);
        }

        // GET: Admin/Proveedores/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();

            var proveedor = await _context.Proveedores
                .FirstOrDefaultAsync(m => m.Id == id);

            if (proveedor == null)
                return NotFound();

            return View(proveedor);
        }

        // GET: Admin/Proveedores/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Admin/Proveedores/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Proveedor proveedor)
        {
            if (!ModelState.IsValid)
                return View(proveedor);

            _context.Add(proveedor);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Proveedor creado exitosamente";
            return RedirectToAction(nameof(Index));
        }

        // GET: Admin/Proveedores/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();

            var proveedor = await _context.Proveedores.FindAsync(id);
            if (proveedor == null)
                return NotFound();

            return View(proveedor);
        }

        // POST: Admin/Proveedores/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Proveedor proveedor)
        {
            if (id != proveedor.Id)
                return NotFound();

            if (!ModelState.IsValid)
                return View(proveedor);

            try
            {
                _context.Update(proveedor);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Proveedor actualizado exitosamente";
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ProveedorExists(proveedor.Id))
                    return NotFound();
                throw;
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: Admin/Proveedores/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
                return NotFound();

            var proveedor = await _context.Proveedores
                .FirstOrDefaultAsync(m => m.Id == id);

            if (proveedor == null)
                return NotFound();

            return View(proveedor);
        }

        // POST: Admin/Proveedores/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var proveedor = await _context.Proveedores.FindAsync(id);
            if (proveedor != null)
            {
                proveedor.Activo = false;
                await _context.SaveChangesAsync();
                TempData["Success"] = "Proveedor eliminado exitosamente";
            }

            return RedirectToAction(nameof(Index));
        }

        private bool ProveedorExists(int id)
        {
            return _context.Proveedores.Any(e => e.Id == id);
        }
    }
}