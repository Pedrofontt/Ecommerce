using Microsoft.EntityFrameworkCore;
using EcommerceSystem.Data;
using EcommerceSystem.Models.Entities;
using EcommerceSystem.Services.Interfaces;

namespace EcommerceSystem.Services.Implementations
{
    public class ProductoService : IProductoService
    {
        private readonly ApplicationDbContext _context;

        public ProductoService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<Producto>> ObtenerTodosAsync()
        {
            return await _context.Productos
                .Include(p => p.Categoria)
                .Include(p => p.Marca)
                .Include(p => p.Proveedor)
                .Where(p => p.Activo)
                .OrderBy(p => p.Nombre)
                .ToListAsync();
        }

        public async Task<Producto?> ObtenerPorIdAsync(int id)
        {
            return await _context.Productos
                .Include(p => p.Categoria)
                .Include(p => p.Marca)
                .Include(p => p.Proveedor)
                .Include(p => p.Imagenes)
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<Producto?> ObtenerPorSKUAsync(string sku)
        {
            return await _context.Productos
                .FirstOrDefaultAsync(p => p.SKU == sku);
        }

        public async Task<List<Producto>> BuscarAsync(string termino)
        {
            return await _context.Productos
                .Include(p => p.Categoria)
                .Include(p => p.Marca)
                .Where(p => p.Activo &&
                    (p.Nombre.Contains(termino) ||
                     p.Descripcion.Contains(termino) ||
                     p.SKU.Contains(termino)))
                .ToListAsync();
        }

        public async Task<List<Producto>> ObtenerPorCategoriaAsync(int categoriaId)
        {
            return await _context.Productos
                .Include(p => p.Categoria)
                .Include(p => p.Marca)
                .Where(p => p.Activo && p.CategoriaId == categoriaId)
                .ToListAsync();
        }

        public async Task<List<Producto>> ObtenerDestacadosAsync()
        {
            return await _context.Productos
                .Include(p => p.Categoria)
                .Include(p => p.Marca)
                .Where(p => p.Activo && p.Destacado)
                .Take(10)
                .ToListAsync();
        }

        public async Task<bool> CrearAsync(Producto producto)
        {
            try
            {
                // Generar SKU si no existe
                if (string.IsNullOrEmpty(producto.SKU))
                {
                    producto.SKU = await GenerarSKUAsync();
                }

                producto.FechaCreacion = DateTime.Now;
                _context.Productos.Add(producto);
                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> ActualizarAsync(Producto producto)
        {
            try
            {
                producto.FechaModificacion = DateTime.Now;
                _context.Productos.Update(producto);
                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> EliminarAsync(int id)
        {
            try
            {
                var producto = await ObtenerPorIdAsync(id);
                if (producto == null) return false;

                // Soft delete
                producto.Activo = false;
                await ActualizarAsync(producto);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> ExisteSKUAsync(string sku, int? productoId = null)
        {
            return await _context.Productos
                .AnyAsync(p => p.SKU == sku && (!productoId.HasValue || p.Id != productoId.Value));
        }

        public async Task<List<Producto>> ObtenerStockBajoAsync()
        {
            return await _context.Productos
                .Include(p => p.Categoria)
                .Include(p => p.Marca)
                .Where(p => p.Activo && p.Stock <= p.StockMinimo)
                .ToListAsync();
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