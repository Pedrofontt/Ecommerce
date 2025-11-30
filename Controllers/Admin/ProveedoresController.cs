using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EcommerceSystem.Data;
using EcommerceSystem.Models.Entities;

namespace EcommerceSystem.Controllers.Admin
{
    [Authorize(Roles = "Admin")]
    public class ProveedoresController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ProveedoresController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Proveedores
        public async Task<IActionResult> Index()
        {
            var proveedores = await _context.Proveedores
                .Where(p => p.Activo)
                .OrderBy(p => p.Nombre)
                .ToListAsync();

            return View("~/Views/Admin/Proveedores/Index.cshtml", proveedores);
        }

        // GET: Proveedores/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();

            var proveedor = await _context.Proveedores
                .Include(p => p.Productos)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (proveedor == null)
                return NotFound();

            return View("~/Views/Admin/Proveedores/Details.cshtml", proveedor);
        }

        // GET: Proveedores/Create
        public IActionResult Create()
        {
            return View("~/Views/Admin/Proveedores/Create.cshtml");
        }

        // POST: Proveedores/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Proveedor proveedor)
        {
            if (!ModelState.IsValid)
            {
                return View("~/Views/Admin/Proveedores/Create.cshtml", proveedor);
            }

            try
            {
                _context.Add(proveedor);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Proveedor creado exitosamente";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception)
            {
                TempData["Error"] = "Error al crear el proveedor";
                return View("~/Views/Admin/Proveedores/Create.cshtml", proveedor);
            }
        }

        // GET: Proveedores/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();

            var proveedor = await _context.Proveedores.FindAsync(id);
            if (proveedor == null)
                return NotFound();

            return View("~/Views/Admin/Proveedores/Edit.cshtml", proveedor);
        }

        // POST: Proveedores/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Proveedor proveedor)
        {
            if (id != proveedor.Id)
                return NotFound();

            if (!ModelState.IsValid)
            {
                return View("~/Views/Admin/Proveedores/Edit.cshtml", proveedor);
            }

            try
            {
                _context.Update(proveedor);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Proveedor actualizado exitosamente";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ProveedorExists(proveedor.Id))
                    return NotFound();
                else
                    throw;
            }
            catch (Exception)
            {
                TempData["Error"] = "Error al actualizar el proveedor";
                return View("~/Views/Admin/Proveedores/Edit.cshtml", proveedor);
            }
        }

        // GET: Proveedores/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
                return NotFound();

            var proveedor = await _context.Proveedores
                .Include(p => p.Productos)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (proveedor == null)
                return NotFound();

            return View("~/Views/Admin/Proveedores/Delete.cshtml", proveedor);
        }

        // POST: Proveedores/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                var proveedor = await _context.Proveedores.FindAsync(id);
                if (proveedor == null)
                {
                    TempData["Error"] = "Proveedor no encontrado";
                    return RedirectToAction(nameof(Index));
                }

                // Soft delete
                proveedor.Activo = false;
                _context.Update(proveedor);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Proveedor eliminado exitosamente";
            }
            catch (Exception)
            {
                TempData["Error"] = "Error al eliminar el proveedor. Puede tener productos asociados.";
            }

            return RedirectToAction(nameof(Index));
        }

        private bool ProveedorExists(int id)
        {
            return _context.Proveedores.Any(e => e.Id == id);
        }
    }
}