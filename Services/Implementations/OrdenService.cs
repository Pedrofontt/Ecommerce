using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using EcommerceSystem.Data;
using EcommerceSystem.Models.Entities;
using EcommerceSystem.Services.Interfaces;

namespace EcommerceSystem.Services.Implementations
{
    public class OrdenService : IOrdenService
    {
        private readonly ApplicationDbContext _context;
        private readonly IKardexService _kardexService;
        private readonly IAlertaService _alertaService;
        private readonly ILogger<OrdenService> _logger;

        public OrdenService(
            ApplicationDbContext context,
            IKardexService kardexService,
            IAlertaService alertaService,
            ILogger<OrdenService> logger)
        {
            _context = context;
            _kardexService = kardexService;
            _alertaService = alertaService;
            _logger = logger;
        }

        public async Task<(bool Success, string Message, Orden? Orden)> CrearOrdenAsync(Orden orden)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // 1. Validar stock para todos los productos
                foreach (var detalle in orden.Detalles)
                {
                    var producto = await _context.Productos.FindAsync(detalle.ProductoId);
                    if (producto == null)
                        return (false, $"Producto {detalle.ProductoId} no encontrado", null);

                    if (producto.Stock < detalle.Cantidad)
                        return (false, $"Stock insuficiente para {producto.Nombre}. Disponible: {producto.Stock}", null);
                }

                // 2. Generar número de orden
                orden.NumeroOrden = await GenerarNumeroOrdenAsync();

                // 3. Calcular totales
                orden.Subtotal = orden.Detalles.Sum(d => d.Cantidad * d.PrecioUnitario);
                orden.Total = orden.Subtotal - orden.Descuento + orden.Impuesto + orden.CostoEnvio;

                foreach (var detalle in orden.Detalles)
                {
                    detalle.Subtotal = detalle.Cantidad * detalle.PrecioUnitario - detalle.Descuento;
                }

                // 4. Guardar orden
                _context.Ordenes.Add(orden);
                await _context.SaveChangesAsync();

                // 5. Ajustar inventario y registrar Kardex
                foreach (var detalle in orden.Detalles)
                {
                    var producto = await _context.Productos.FindAsync(detalle.ProductoId);
                    producto.Stock -= detalle.Cantidad;
                    _context.Productos.Update(producto);

                    // Registrar movimiento en Kardex
                    await _kardexService.RegistrarMovimientoAsync(
                        producto.Id,
                        "SALIDA_VENTA",
                        detalle.Cantidad,
                        $"Orden #{orden.NumeroOrden}",
                        $"Venta de {detalle.Cantidad} unidades"
                    );

                    // Verificar alertas de stock
                    await _alertaService.VerificarStockYCrearAlertasAsync(producto.Id);
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return (true, "Orden creada exitosamente", orden);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return (false, $"Error: {ex.Message}", null);
            }
        }

        public async Task<Orden?> ObtenerPorIdAsync(int id)
        {
            return await _context.Ordenes
                .Include(o => o.Cliente)
                .Include(o => o.Detalles)
                    .ThenInclude(d => d.Producto)
                .FirstOrDefaultAsync(o => o.Id == id);
        }

        public async Task<List<Orden>> ObtenerPorClienteAsync(int clienteId)
        {
            return await _context.Ordenes
                .Include(o => o.Detalles)
                .Where(o => o.ClienteId == clienteId)
                .OrderByDescending(o => o.FechaOrden)
                .ToListAsync();
        }

        public async Task<bool> CambiarEstadoAsync(int ordenId, string nuevoEstado)
        {
            try
            {
                var orden = await ObtenerPorIdAsync(ordenId);
                if (orden == null) return false;

                // Validar transiciones permitidas
                var transicionValida = nuevoEstado switch
                {
                    "Confirmado" => orden.Estado == "Pendiente",
                    "Enviado" => orden.Estado == "Confirmado",
                    "Entregado" => orden.Estado == "Enviado",
                    "Cancelado" => orden.Estado == "Pendiente" || orden.Estado == "Confirmado",
                    _ => false
                };

                if (!transicionValida)
                    return false;

                orden.Estado = nuevoEstado;

                // Actualizar fechas según estado
                switch (nuevoEstado)
                {
                    case "Confirmado":
                        orden.FechaPago = DateTime.Now;
                        break;
                    case "Enviado":
                        orden.FechaEnvio = DateTime.Now;
                        break;
                    case "Entregado":
                        orden.FechaEntrega = DateTime.Now;
                        break;
                }

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cambiar estado de orden ID: {OrdenId} a {Estado}", ordenId, nuevoEstado);
                return false;
            }
        }

        public async Task<bool> CancelarOrdenAsync(int ordenId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var orden = await ObtenerPorIdAsync(ordenId);
                if (orden == null || orden.Estado == "Cancelado")
                    return false;

                // Devolver stock
                foreach (var detalle in orden.Detalles)
                {
                    var producto = await _context.Productos.FindAsync(detalle.ProductoId);
                    producto.Stock += detalle.Cantidad;
                    _context.Productos.Update(producto);

                    await _kardexService.RegistrarMovimientoAsync(
                        producto.Id,
                        "DEVOLUCION",
                        detalle.Cantidad,
                        $"Cancelación Orden #{orden.NumeroOrden}",
                        "Devolución por cancelación de orden"
                    );
                }

                orden.Estado = "Cancelado";
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cancelar orden ID: {OrdenId}", ordenId);
                await transaction.RollbackAsync();
                return false;
            }
        }

        public async Task<string> GenerarNumeroOrdenAsync()
        {
            var fecha = DateTime.Now.ToString("yyyyMMdd");
            var ultimaOrden = await _context.Ordenes
                .Where(o => o.NumeroOrden.StartsWith(fecha))
                .OrderByDescending(o => o.Id)
                .FirstOrDefaultAsync();

            int secuencia = 1;
            if (ultimaOrden != null)
            {
                var partes = ultimaOrden.NumeroOrden.Split('-');
                if (partes.Length == 2 && int.TryParse(partes[1], out int num))
                    secuencia = num + 1;
            }

            return $"{fecha}-{secuencia:D4}";
        }
    }
}