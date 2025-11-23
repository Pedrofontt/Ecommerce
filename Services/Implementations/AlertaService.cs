using Microsoft.EntityFrameworkCore;
using EcommerceSystem.Data;
using EcommerceSystem.Models.Entities;
using EcommerceSystem.Services.Interfaces;

namespace EcommerceSystem.Services.Implementations
{
    public class AlertaService : IAlertaService
    {
        private readonly ApplicationDbContext _context;

        public AlertaService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task VerificarStockYCrearAlertasAsync(int productoId)
        {
            var producto = await _context.Productos.FindAsync(productoId);
            if (producto == null) return;

            string tipo = "";
            string mensaje = "";

            if (producto.Stock == 0)
            {
                tipo = "StockAgotado";
                mensaje = $"Producto {producto.Nombre} está agotado";
            }
            else if (producto.Stock <= producto.StockMinimo * 0.5)
            {
                tipo = "StockCritico";
                mensaje = $"Producto {producto.Nombre} en stock crítico ({producto.Stock} unidades)";
            }
            else if (producto.Stock <= producto.StockMinimo)
            {
                tipo = "StockBajo";
                mensaje = $"Producto {producto.Nombre} alcanzó stock mínimo ({producto.Stock} unidades)";
            }
            else
            {
                return; // Stock suficiente, no crear alerta
            }

            // Verificar si ya existe una alerta pendiente para este producto
            var alertaExistente = await _context.AlertasStock
                .FirstOrDefaultAsync(a => a.ProductoId == productoId && a.Estado == "Pendiente");

            if (alertaExistente == null)
            {
                var alerta = new AlertaStock
                {
                    ProductoId = productoId,
                    Tipo = tipo,
                    Mensaje = mensaje,
                    Fecha = DateTime.Now,
                    Estado = "Pendiente"
                };

                _context.AlertasStock.Add(alerta);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<List<AlertaStock>> ObtenerAlertasPendientesAsync()
        {
            return await _context.AlertasStock
                .Include(a => a.Producto)
                .Where(a => a.Estado == "Pendiente")
                .OrderByDescending(a => a.Fecha)
                .ToListAsync();
        }

        public async Task<bool> MarcarComoRevisadaAsync(int alertaId, string usuarioId)
        {
            try
            {
                var alerta = await _context.AlertasStock.FindAsync(alertaId);
                if (alerta == null) return false;

                alerta.Estado = "Revisado";
                alerta.FechaRevision = DateTime.Now;
                alerta.RevisadoPor = usuarioId;

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