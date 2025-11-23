using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EcommerceSystem.Data;
using EcommerceSystem.Models.Entities;
using EcommerceSystem.Utilities;

namespace EcommerceSystem.Controllers
{
    public class TiendaController : Controller
    {
        private readonly ApplicationDbContext _context;

        public TiendaController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Tienda
        public async Task<IActionResult> Index(
            int? categoriaId,
            int? marcaId,
            decimal? precioMin,
            decimal? precioMax,
            string busqueda,
            string orden,
            int? pageNumber)
        {
            var productos = _context.Productos
                .Include(p => p.Categoria)
                .Include(p => p.Marca)
                .Where(p => p.Activo && p.Stock > 0)
                .AsQueryable();

            // Filtros
            if (categoriaId.HasValue)
            {
                productos = productos.Where(p => p.CategoriaId == categoriaId.Value);
                ViewData["CategoriaId"] = categoriaId.Value;
            }

            if (marcaId.HasValue)
            {
                productos = productos.Where(p => p.MarcaId == marcaId.Value);
                ViewData["MarcaId"] = marcaId.Value;
            }

            if (precioMin.HasValue)
            {
                productos = productos.Where(p => p.Precio >= precioMin.Value);
                ViewData["PrecioMin"] = precioMin.Value;
            }

            if (precioMax.HasValue)
            {
                productos = productos.Where(p => p.Precio <= precioMax.Value);
                ViewData["PrecioMax"] = precioMax.Value;
            }

            if (!string.IsNullOrEmpty(busqueda))
            {
                productos = productos.Where(p =>
                    p.Nombre.Contains(busqueda) ||
                    (p.Descripcion != null && p.Descripcion.Contains(busqueda)));
                ViewData["Busqueda"] = busqueda;
            }

            // Ordenamiento
            productos = orden switch
            {
                "precio_asc" => productos.OrderBy(p => p.Precio),
                "precio_desc" => productos.OrderByDescending(p => p.Precio),
                "nombre" => productos.OrderBy(p => p.Nombre),
                "nuevo" => productos.OrderByDescending(p => p.FechaCreacion),
                _ => productos.OrderBy(p => p.Nombre)
            };

            ViewData["Orden"] = orden;

            // Categorías y Marcas para filtros
            ViewBag.Categorias = await _context.Categorias
                .Where(c => c.Activo)
                .OrderBy(c => c.Nombre)
                .ToListAsync();

            ViewBag.Marcas = await _context.Marcas
                .Where(m => m.Activo)
                .OrderBy(m => m.Nombre)
                .ToListAsync();

            int pageSize = 12;
            return View(await PaginatedList<Producto>.CreateAsync(
                productos.AsNoTracking(),
                pageNumber ?? 1,
                pageSize));
        }

        // GET: Tienda/Producto/5
        public async Task<IActionResult> Producto(int? id)
        {
            if (id == null)
                return NotFound();

            var producto = await _context.Productos
                .Include(p => p.Categoria)
                .Include(p => p.Marca)
                .Include(p => p.Imagenes)
                .FirstOrDefaultAsync(p => p.Id == id && p.Activo);

            if (producto == null)
                return NotFound();

            // Productos relacionados
            ViewBag.ProductosRelacionados = await _context.Productos
                .Include(p => p.Categoria)
                .Where(p => p.Activo &&
                           p.Stock > 0 &&
                           p.Id != id &&
                           p.CategoriaId == producto.CategoriaId)
                .Take(4)
                .ToListAsync();

            return View(producto);
        }

        // GET: Tienda/Categoria/5
        public async Task<IActionResult> Categoria(int? id, int? pageNumber)
        {
            if (id == null)
                return NotFound();

            var categoria = await _context.Categorias
                .Include(c => c.Subcategorias)
                .FirstOrDefaultAsync(c => c.Id == id && c.Activo);

            if (categoria == null)
                return NotFound();

            ViewBag.Categoria = categoria;

            var productos = _context.Productos
                .Include(p => p.Categoria)
                .Include(p => p.Marca)
                .Where(p => p.Activo && p.Stock > 0 && p.CategoriaId == id);

            int pageSize = 12;
            return View(await PaginatedList<Producto>.CreateAsync(
                productos.AsNoTracking(),
                pageNumber ?? 1,
                pageSize));
        }

        // GET: Tienda/Buscar
        public async Task<IActionResult> Buscar(string q, int? pageNumber)
        {
            if (string.IsNullOrWhiteSpace(q))
                return RedirectToAction(nameof(Index));

            ViewData["Query"] = q;

            var productos = _context.Productos
                .Include(p => p.Categoria)
                .Include(p => p.Marca)
                .Where(p => p.Activo && p.Stock > 0 &&
                    (p.Nombre.Contains(q) ||
                     (p.Descripcion != null && p.Descripcion.Contains(q)) ||
                     p.SKU.Contains(q)));

            int pageSize = 12;
            return View(await PaginatedList<Producto>.CreateAsync(
                productos.AsNoTracking(),
                pageNumber ?? 1,
                pageSize));
        }
    }
}