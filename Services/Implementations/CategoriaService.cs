using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using EcommerceSystem.Data;
using EcommerceSystem.Models.Entities;
using EcommerceSystem.Services.Interfaces;

namespace EcommerceSystem.Services.Implementations
{
    public class CategoriaService : ICategoriaService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<CategoriaService> _logger;

        public CategoriaService(ApplicationDbContext context, ILogger<CategoriaService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<List<Categoria>> ObtenerTodasAsync()
        {
            return await _context.Categorias
                .Include(c => c.Subcategorias)
                .Where(c => c.Activo)
                .OrderBy(c => c.Orden)
                .ToListAsync();
        }

        public async Task<Categoria?> ObtenerPorIdAsync(int id)
        {
            return await _context.Categorias
                .Include(c => c.Subcategorias)
                .FirstOrDefaultAsync(c => c.Id == id);
        }

        public async Task<bool> CrearAsync(Categoria categoria)
        {
            try
            {
                _context.Categorias.Add(categoria);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear categoría: {Nombre}", categoria.Nombre);
                return false;
            }
        }

        public async Task<bool> ActualizarAsync(Categoria categoria)
        {
            try
            {
                _context.Categorias.Update(categoria);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar categoría ID: {Id}", categoria.Id);
                return false;
            }
        }

        public async Task<bool> EliminarAsync(int id)
        {
            try
            {
                var categoria = await ObtenerPorIdAsync(id);
                if (categoria == null) return false;

                categoria.Activo = false;
                await ActualizarAsync(categoria);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar categoría ID: {Id}", id);
                return false;
            }
        }
    }
}