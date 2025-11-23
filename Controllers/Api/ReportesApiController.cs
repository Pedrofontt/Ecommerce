using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using EcommerceSystem.Data;
using EcommerceSystem.Services.Interfaces;

namespace EcommerceSystem.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin,Vendedor")]
    public class ReportesApiController : ControllerBase
    {
        private readonly IReporteService _reporteService;
        private readonly IAlertaService _alertaService;
        private readonly ApplicationDbContext _context;

        public ReportesApiController(
            IReporteService reporteService,
            IAlertaService alertaService,
            ApplicationDbContext context)
        {
            _reporteService = reporteService;
            _alertaService = alertaService;
            _context = context;
        }

        /// <summary>
        /// Obtener resumen del dashboard
        /// </summary>
        [HttpGet("dashboard")]
        public async Task<ActionResult> GetDashboard()
        {
            var dashboard = await _reporteService.ObtenerDashboardAsync();

            return Ok(new
            {
                ventas = new
                {
                    hoy = dashboard.VentasHoy,
                    semana = dashboard.VentasSemana,
                    mes = dashboard.VentasMes
                },
                ordenes = new
                {
                    pendientes = dashboard.OrdenesPendientes,
                    totalMes = dashboard.TotalOrdenesMes
                },
                productos = new
                {
                    stockBajo = dashboard.ProductosStockBajo,
                    activos = dashboard.ProductosActivos
                },
                clientes = new
                {
                    total = dashboard.TotalClientes,
                    nuevosMes = dashboard.ClientesNuevosMes
                },
                tendencias = dashboard.VentasPorDia?.Select(v => new
                {
                    fecha = v.Fecha.ToString("yyyy-MM-dd"),
                    total = v.Total,
                    cantidad = v.CantidadOrdenes
                }).ToList()
            });
        }

        /// <summary>
        /// Obtener productos más vendidos
        /// </summary>
        [HttpGet("productos-mas-vendidos")]
        public async Task<ActionResult> GetProductosMasVendidos([FromQuery] int top = 10)
        {
            var productos = await _reporteService.ObtenerProductosMasVendidosAsync(top);

            return Ok(productos.Select(p => new
            {
                productoId = p.ProductoId,
                nombre = p.Nombre,
                cantidadVendida = p.CantidadVendida,
                totalVentas = p.TotalVentas
            }));
        }

        /// <summary>
        /// Obtener ventas por período
        /// </summary>
        [HttpGet("ventas")]
        public async Task<ActionResult> GetVentasPorPeriodo(
            [FromQuery] DateTime? desde = null,
            [FromQuery] DateTime? hasta = null)
        {
            var inicio = desde ?? DateTime.Today.AddDays(-30);
            var fin = hasta ?? DateTime.Today;

            var ventas = await _reporteService.ObtenerVentasPorPeriodoAsync(inicio, fin);

            return Ok(new
            {
                periodo = new { inicio = inicio.ToString("yyyy-MM-dd"), fin = fin.ToString("yyyy-MM-dd") },
                totalVentas = ventas.Sum(v => v.Total),
                totalOrdenes = ventas.Sum(v => v.CantidadOrdenes),
                promedioDiario = ventas.Any() ? ventas.Average(v => v.Total) : 0,
                detallePorDia = ventas.Select(v => new
                {
                    fecha = v.Fecha.ToString("yyyy-MM-dd"),
                    total = v.Total,
                    cantidadOrdenes = v.CantidadOrdenes
                }).ToList()
            });
        }

        /// <summary>
        /// Obtener reporte de inventario actual
        /// </summary>
        [HttpGet("inventario")]
        public async Task<ActionResult> GetReporteInventario()
        {
            var productos = await _context.Productos
                .Include(p => p.Categoria)
                .Include(p => p.Marca)
                .Where(p => p.Activo)
                .OrderBy(p => p.Stock)
                .ToListAsync();

            var stockBajo = productos.Where(p => p.Stock <= p.StockMinimo).ToList();
            var sinStock = productos.Where(p => p.Stock == 0).ToList();

            return Ok(new
            {
                resumen = new
                {
                    totalProductos = productos.Count,
                    productosStockBajo = stockBajo.Count,
                    productosSinStock = sinStock.Count,
                    valorInventario = productos.Sum(p => p.Precio * p.Stock)
                },
                stockBajo = stockBajo.Select(p => new
                {
                    id = p.Id,
                    sku = p.SKU,
                    nombre = p.Nombre,
                    categoria = p.Categoria?.Nombre,
                    stockActual = p.Stock,
                    stockMinimo = p.StockMinimo,
                    diferencia = p.StockMinimo - p.Stock
                }),
                sinStock = sinStock.Select(p => new
                {
                    id = p.Id,
                    sku = p.SKU,
                    nombre = p.Nombre,
                    categoria = p.Categoria?.Nombre
                })
            });
        }

        /// <summary>
        /// Obtener historial de ventas por categoría
        /// </summary>
        [HttpGet("ventas-por-categoria")]
        public async Task<ActionResult> GetVentasPorCategoria(
            [FromQuery] DateTime? desde = null,
            [FromQuery] DateTime? hasta = null)
        {
            var inicio = desde ?? DateTime.Today.AddMonths(-1);
            var fin = hasta ?? DateTime.Today;

            var ventas = await _context.OrdenDetalles
                .Include(d => d.Producto)
                    .ThenInclude(p => p!.Categoria)
                .Include(d => d.Orden)
                .Where(d => d.Orden.FechaOrden >= inicio &&
                           d.Orden.FechaOrden <= fin &&
                           d.Orden.Estado != "Cancelado")
                .GroupBy(d => new { d.Producto!.CategoriaId, d.Producto.Categoria!.Nombre })
                .Select(g => new
                {
                    categoriaId = g.Key.CategoriaId,
                    categoriaNombre = g.Key.Nombre ?? "Sin categoría",
                    cantidadProductos = g.Sum(d => d.Cantidad),
                    totalVentas = g.Sum(d => d.Subtotal)
                })
                .OrderByDescending(x => x.totalVentas)
                .ToListAsync();

            return Ok(ventas);
        }

        /// <summary>
        /// Obtener historial de clientes con sus compras
        /// </summary>
        [HttpGet("clientes")]
        public async Task<ActionResult> GetReporteClientes()
        {
            var clientes = await _context.Clientes
                .Include(c => c.Ordenes)
                .OrderByDescending(c => c.Ordenes.Count)
                .ToListAsync();

            return Ok(clientes.Select(c => new
            {
                id = c.Id,
                nombre = c.NombreCompleto,
                email = c.Email,
                telefono = c.Telefono,
                fechaRegistro = c.FechaRegistro,
                totalOrdenes = c.Ordenes.Count,
                totalCompras = c.Ordenes.Where(o => o.Estado != "Cancelado").Sum(o => o.Total),
                ultimaCompra = c.Ordenes.OrderByDescending(o => o.FechaOrden).FirstOrDefault()?.FechaOrden
            }));
        }

        /// <summary>
        /// Obtener alertas de stock pendientes
        /// </summary>
        [HttpGet("alertas")]
        public async Task<ActionResult> GetAlertasStock()
        {
            var alertas = await _alertaService.ObtenerAlertasPendientesAsync();

            return Ok(alertas.Select(a => new
            {
                id = a.Id,
                productoId = a.ProductoId,
                productoNombre = a.Producto?.Nombre,
                productoSKU = a.Producto?.SKU,
                stockActual = a.StockActual,
                stockMinimo = a.StockMinimo,
                fechaAlerta = a.FechaAlerta,
                revisado = a.Revisado
            }));
        }

        /// <summary>
        /// Marcar alerta como revisada
        /// </summary>
        [HttpPatch("alertas/{id}/revisar")]
        public async Task<ActionResult> MarcarAlertaRevisada(int id)
        {
            var usuarioId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "system";
            var exito = await _alertaService.MarcarComoRevisadaAsync(id, usuarioId);

            if (!exito)
                return NotFound(new { message = "Alerta no encontrada" });

            return Ok(new { message = "Alerta marcada como revisada" });
        }

        /// <summary>
        /// Obtener movimientos de inventario (Kardex)
        /// </summary>
        [HttpGet("kardex")]
        public async Task<ActionResult> GetMovimientosKardex(
            [FromQuery] int? productoId = null,
            [FromQuery] DateTime? desde = null,
            [FromQuery] DateTime? hasta = null,
            [FromQuery] int limite = 100)
        {
            var query = _context.Kardex
                .Include(k => k.Producto)
                .AsQueryable();

            if (productoId.HasValue)
                query = query.Where(k => k.ProductoId == productoId.Value);

            if (desde.HasValue)
                query = query.Where(k => k.Fecha >= desde.Value);

            if (hasta.HasValue)
                query = query.Where(k => k.Fecha <= hasta.Value);

            var movimientos = await query
                .OrderByDescending(k => k.Fecha)
                .Take(limite)
                .ToListAsync();

            return Ok(movimientos.Select(k => new
            {
                id = k.Id,
                productoId = k.ProductoId,
                productoNombre = k.Producto?.Nombre,
                tipoMovimiento = k.TipoMovimiento,
                cantidad = k.Cantidad,
                stockAnterior = k.StockAnterior,
                stockNuevo = k.StockNuevo,
                referencia = k.Referencia,
                fecha = k.Fecha
            }));
        }
    }
}
