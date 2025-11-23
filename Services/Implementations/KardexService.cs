using Microsoft.EntityFrameworkCore;
using EcommerceSystem.Data;
using EcommerceSystem.Models.Entities;
using EcommerceSystem.Services.Interfaces;

namespace EcommerceSystem.Services.Implementations
{
    public class KardexService : IKardexService
    {
        private readonly ApplicationDbContext _context;

        public KardexService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task RegistrarMovimientoAsync(
            int productoId,
            string tipo,
            int cantidad,
            string referencia,
            string? descripcion = null)
        {
            var producto = await _context.Productos.FindAsync(productoId);
            if (producto == null) return;

            var kardex = new Kardex
            {
                ProductoId = productoId,
                TipoMovimiento = tipo,
                Cantidad = cantidad,
                StockAnterior = producto.Stock + (tipo.Contains("SALIDA") ? cantidad : -cantidad),
                StockNuevo = producto.Stock,
                Fecha = DateTime.Now,
                Referencia = referencia,
                Descripcion = descripcion
            };

            _context.Kardex.Add(kardex);
            await _context.SaveChangesAsync();
        }

        public async Task<List<Kardex>> ObtenerPorProductoAsync(int productoId)
        {
            return await _context.Kardex
                .Where(k => k.ProductoId == productoId)
                .OrderByDescending(k => k.Fecha)
                .ToListAsync();
        }

        public async Task<List<Kardex>> ObtenerPorFechaAsync(DateTime fechaInicio, DateTime fechaFin)
        {
            return await _context.Kardex
                .Include(k => k.Producto)
                .Where(k => k.Fecha >= fechaInicio && k.Fecha <= fechaFin)
                .OrderByDescending(k => k.Fecha)
                .ToListAsync();
        }
    }
}