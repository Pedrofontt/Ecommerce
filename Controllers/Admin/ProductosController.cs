using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using EcommerceSystem.Data;
using EcommerceSystem.Models.Entities;
using EcommerceSystem.Models.ViewModels;
using EcommerceSystem.Services.Interfaces;
using EcommerceSystem.Utilities;
using Microsoft.EntityFrameworkCore;

namespace EcommerceSystem.Controllers.Admin
{
    [Authorize(Roles = "Admin")]
    // ❌ ELIMINADO: [Area("Admin")] - Para que funcione como DashboardController
    public class ProductosController : Controller
    {
        private readonly IProductoService _productoService;
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;

        public ProductosController(
            IProductoService productoService,
            ApplicationDbContext context,
            IWebHostEnvironment env)
        {
            _productoService = productoService;
            _context = context;
            _env = env;
        }

        // GET: Productos (ruta: /Productos)
        public async Task<IActionResult> Index(
            string sortOrder,
            string currentFilter,
            string searchString,
            int? pageNumber)
        {
            ViewData["CurrentSort"] = sortOrder;
            ViewData["NombreSort"] = String.IsNullOrEmpty(sortOrder) ? "nombre_desc" : "";
            ViewData["PrecioSort"] = sortOrder == "precio_asc" ? "precio_desc" : "precio_asc";
            ViewData["StockSort"] = sortOrder == "stock_asc" ? "stock_desc" : "stock_asc";

            if (searchString != null)
            {
                pageNumber = 1;
            }
            else
            {
                searchString = currentFilter;
            }

            ViewData["CurrentFilter"] = searchString;

            var productos = _context.Productos
                .Include(p => p.Categoria)
                .Include(p => p.Marca)
                .Include(p => p.Proveedor)
                .AsQueryable();

            // Búsqueda
            if (!String.IsNullOrEmpty(searchString))
            {
                productos = productos.Where(p =>
                    p.Nombre.Contains(searchString) ||
                    p.SKU.Contains(searchString) ||
                    (p.Categoria != null && p.Categoria.Nombre.Contains(searchString)) ||
                    (p.Marca != null && p.Marca.Nombre.Contains(searchString))
                );
            }

            // Ordenamiento
            productos = sortOrder switch
            {
                "nombre_desc" => productos.OrderByDescending(p => p.Nombre),
                "precio_asc" => productos.OrderBy(p => p.Precio),
                "precio_desc" => productos.OrderByDescending(p => p.Precio),
                "stock_asc" => productos.OrderBy(p => p.Stock),
                "stock_desc" => productos.OrderByDescending(p => p.Stock),
                _ => productos.OrderBy(p => p.Nombre)
            };

            int pageSize = 10;

            // ✅ Especificar ruta completa de vista como en DashboardController
            return View("~/Views/Admin/Productos/Index.cshtml",
                await PaginatedList<Producto>.CreateAsync(
                    productos.AsNoTracking(),
                    pageNumber ?? 1,
                    pageSize));
        }

        // GET: Productos/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();

            var producto = await _productoService.ObtenerPorIdAsync(id.Value);
            if (producto == null)
                return NotFound();

            return View("~/Views/Admin/Productos/Details.cshtml", producto);
        }

        // GET: Productos/Create
        public IActionResult Create()
        {
            var viewModel = new ProductoViewModel();
            CargarDropdowns(viewModel);
            return View("~/Views/Admin/Productos/Create.cshtml", viewModel);
        }

        // POST: Productos/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ProductoViewModel model)
        {
            if (!ModelState.IsValid)
            {
                CargarDropdowns(model);
                return View("~/Views/Admin/Productos/Create.cshtml", model);
            }

            // Crear entidad
            var producto = new Producto
            {
                SKU = await GenerarSKUAsync(),
                Nombre = model.Nombre,
                Descripcion = model.Descripcion,
                DescripcionCorta = model.DescripcionCorta,
                Precio = model.Precio,
                PrecioComparacion = model.PrecioComparacion,
                Stock = model.Stock,
                StockMinimo = model.StockMinimo,
                CategoriaId = model.CategoriaId,
                MarcaId = model.MarcaId,
                ProveedorId = model.ProveedorId,
                Destacado = model.Destacado,
                Activo = model.Activo
            };

            // Guardar imagen
            if (model.ImagenFile != null)
            {
                producto.ImagenPrincipal = await ImageHelper.GuardarImagenAsync(
                    model.ImagenFile, _env, "productos");
            }

            var resultado = await _productoService.CrearAsync(producto);
            if (resultado)
            {
                TempData["Success"] = "Producto creado exitosamente";
                return RedirectToAction(nameof(Index));
            }

            TempData["Error"] = "Error al crear el producto";
            CargarDropdowns(model);
            return View("~/Views/Admin/Productos/Create.cshtml", model);
        }

        // GET: Productos/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();

            var producto = await _productoService.ObtenerPorIdAsync(id.Value);
            if (producto == null)
                return NotFound();

            var viewModel = new ProductoViewModel
            {
                Id = producto.Id,
                Nombre = producto.Nombre,
                Descripcion = producto.Descripcion,
                DescripcionCorta = producto.DescripcionCorta,
                Precio = producto.Precio,
                PrecioComparacion = producto.PrecioComparacion,
                Stock = producto.Stock,
                StockMinimo = producto.StockMinimo,
                CategoriaId = producto.CategoriaId,
                MarcaId = producto.MarcaId,
                ProveedorId = producto.ProveedorId,
                Destacado = producto.Destacado,
                Activo = producto.Activo,
                ImagenActual = producto.ImagenPrincipal
            };

            CargarDropdowns(viewModel);
            return View("~/Views/Admin/Productos/Edit.cshtml", viewModel);
        }

        // POST: Productos/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ProductoViewModel model)
        {
            if (id != model.Id)
                return NotFound();

            if (!ModelState.IsValid)
            {
                CargarDropdowns(model);
                return View("~/Views/Admin/Productos/Edit.cshtml", model);
            }

            var producto = await _productoService.ObtenerPorIdAsync(id);
            if (producto == null)
                return NotFound();

            // Actualizar propiedades
            producto.Nombre = model.Nombre;
            producto.Descripcion = model.Descripcion;
            producto.DescripcionCorta = model.DescripcionCorta;
            producto.Precio = model.Precio;
            producto.PrecioComparacion = model.PrecioComparacion;
            producto.Stock = model.Stock;
            producto.StockMinimo = model.StockMinimo;
            producto.CategoriaId = model.CategoriaId;
            producto.MarcaId = model.MarcaId;
            producto.ProveedorId = model.ProveedorId;
            producto.Destacado = model.Destacado;
            producto.Activo = model.Activo;

            // Actualizar imagen si se subió una nueva
            if (model.ImagenFile != null)
            {
                ImageHelper.EliminarImagen(producto.ImagenPrincipal, _env);
                producto.ImagenPrincipal = await ImageHelper.GuardarImagenAsync(
                    model.ImagenFile, _env, "productos");
            }

            var resultado = await _productoService.ActualizarAsync(producto);
            if (resultado)
            {
                TempData["Success"] = "Producto actualizado exitosamente";
                return RedirectToAction(nameof(Index));
            }

            TempData["Error"] = "Error al actualizar el producto";
            CargarDropdowns(model);
            return View("~/Views/Admin/Productos/Edit.cshtml", model);
        }

        // GET: Productos/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
                return NotFound();

            var producto = await _productoService.ObtenerPorIdAsync(id.Value);
            if (producto == null)
                return NotFound();

            return View("~/Views/Admin/Productos/Delete.cshtml", producto);
        }

        // POST: Productos/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var resultado = await _productoService.EliminarAsync(id);
            if (resultado)
            {
                TempData["Success"] = "Producto eliminado exitosamente";
            }
            else
            {
                TempData["Error"] = "Error al eliminar el producto";
            }

            return RedirectToAction(nameof(Index));
        }

        // Métodos auxiliares
        private void CargarDropdowns(ProductoViewModel model)
        {
            model.Categorias = _context.Categorias
                .Where(c => c.Activo)
                .Select(c => new SelectListItem
                {
                    Value = c.Id.ToString(),
                    Text = c.Nombre
                })
                .ToList();
            model.Categorias.Insert(0, new SelectListItem { Value = "", Text = "-- Seleccione --" });

            model.Marcas = _context.Marcas
                .Where(m => m.Activo)
                .Select(m => new SelectListItem
                {
                    Value = m.Id.ToString(),
                    Text = m.Nombre
                })
                .ToList();
            model.Marcas.Insert(0, new SelectListItem { Value = "", Text = "-- Seleccione --" });

            model.Proveedores = _context.Proveedores
                .Where(p => p.Activo)
                .Select(p => new SelectListItem
                {
                    Value = p.Id.ToString(),
                    Text = p.Nombre
                })
                .ToList();
            model.Proveedores.Insert(0, new SelectListItem { Value = "", Text = "-- Seleccione --" });
        }

        private async Task<string> GenerarSKUAsync()
        {
            var ultimo = await _context.Productos
                .OrderByDescending(p => p.Id)
                .FirstOrDefaultAsync();

            int numero = 1;
            if (ultimo != null && !string.IsNullOrEmpty(ultimo.SKU))
            {
                var partes = ultimo.SKU.Split('-');
                if (partes.Length == 2 && int.TryParse(partes[1], out int n))
                    numero = n + 1;
            }

            return $"SKU-{numero:D6}";
        }
    }
}