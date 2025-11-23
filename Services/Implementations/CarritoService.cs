using Microsoft.EntityFrameworkCore;
using EcommerceSystem.Data;
using EcommerceSystem.Models.Entities;
using EcommerceSystem.Services.Interfaces;

namespace EcommerceSystem.Services.Implementations
{
    public class CarritoService : ICarritoService
    {
        private readonly ApplicationDbContext _context;

        public CarritoService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Carrito?> ObtenerCarritoActualAsync(string? usuarioId, string? sessionId)
        {
            return await _context.Carritos
                .Include(c => c.Items)
                    .ThenInclude(i => i.Producto)
                .FirstOrDefaultAsync(c =>
                    (usuarioId != null && c.UsuarioId == usuarioId) ||
                    (sessionId != null && c.SessionId == sessionId));
        }

        public async Task<bool> AgregarItemAsync(int carritoId, int productoId, int cantidad)
        {
            try
            {
                var item = await _context.CarritoItems
                    .FirstOrDefaultAsync(i => i.CarritoId == carritoId && i.ProductoId == productoId);

                if (item != null)
                {
                    item.Cantidad += cantidad;
                    _context.CarritoItems.Update(item);
                }
                else
                {
                    var nuevoItem = new CarritoItem
                    {
                        CarritoId = carritoId,
                        ProductoId = productoId,
                        Cantidad = cantidad
                    };
                    _context.CarritoItems.Add(nuevoItem);
                }

                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> ActualizarCantidadAsync(int itemId, int cantidad)
        {
            try
            {
                var item = await _context.CarritoItems.FindAsync(itemId);
                if (item == null) return false;

                item.Cantidad = cantidad;
                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> EliminarItemAsync(int itemId)
        {
            try
            {
                var item = await _context.CarritoItems.FindAsync(itemId);
                if (item == null) return false;

                _context.CarritoItems.Remove(item);
                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> VaciarCarritoAsync(int carritoId)
        {
            try
            {
                var items = await _context.CarritoItems
                    .Where(i => i.CarritoId == carritoId)
                    .ToListAsync();

                _context.CarritoItems.RemoveRange(items);
                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}