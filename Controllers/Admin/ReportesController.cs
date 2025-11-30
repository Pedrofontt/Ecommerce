using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EcommerceSystem.Data;
using EcommerceSystem.Services.Interfaces;

namespace EcommerceSystem.Controllers.Admin
{
    [Authorize(Roles = "Admin,Vendedor")]
    public class ReportesController : Controller
    {
        private readonly IReporteService _reporteService;
        private readonly ApplicationDbContext _context;

        public ReportesController(
            IReporteService reporteService,
            ApplicationDbContext context)
        {
            _reporteService = reporteService;
            _context = context;
        }

        // GET: Reportes/Ventas
        public async Task<IActionResult> Ventas(DateTime? fechaInicio, DateTime? fechaFin, string periodo = "mes")
        {
            // Establecer fechas por defecto si no se proporcionan
            if (!fechaInicio.HasValue || !fechaFin.HasValue)
            {
                fechaFin = DateTime.Today;
                fechaInicio = periodo switch
                {
                    "dia" => DateTime.Today,
                    "semana" => DateTime.Today.AddDays(-7),
                    "mes" => DateTime.Today.AddMonths(-1),
                    "anio" => DateTime.Today.AddYears(-1),
                    _ => DateTime.Today.AddMonths(-1)
                };
            }

            ViewData["FechaInicio"] = fechaInicio.Value.ToString("yyyy-MM-dd");
            ViewData["FechaFin"] = fechaFin.Value.ToString("yyyy-MM-dd");
            ViewData["Periodo"] = periodo;

            // Obtener órdenes del período
            var ordenes = await _context.Ordenes
                .Include(o => o.Detalles)
                    .ThenInclude(d => d.Producto)
                        .ThenInclude(p => p.Categoria)
                .Where(o => o.FechaOrden >= fechaInicio.Value &&
                           o.FechaOrden <= fechaFin.Value &&
                           o.Estado != "Cancelado")
                .ToListAsync();

            // Calcular estadísticas
            var totalVentas = ordenes.Sum(o => o.Total);
            var totalOrdenes = ordenes.Count;
            var promedioVenta = totalOrdenes > 0 ? totalVentas / totalOrdenes : 0;

            // Ventas por día
            var ventasPorDia = ordenes
                .GroupBy(o => o.FechaOrden.Date)
                .Select(g => new
                {
                    Fecha = g.Key,
                    FechaStr = g.Key.ToString("dd/MM/yyyy"),
                    Total = g.Sum(o => o.Total),
                    Cantidad = g.Count(),
                    Promedio = g.Average(o => o.Total)
                })
                .OrderBy(v => v.Fecha)
                .ToList();

            // Productos más vendidos
            var productosMasVendidos = ordenes
                .SelectMany(o => o.Detalles)
                .GroupBy(d => new { d.ProductoId, d.Producto.Nombre })
                .Select(g => new
                {
                    Producto = g.Key.Nombre,
                    Cantidad = g.Sum(d => d.Cantidad),
                    Total = g.Sum(d => d.Subtotal)
                })
                .OrderByDescending(p => p.Cantidad)
                .Take(10)
                .ToList();

            // Ventas por categoría
            var ventasPorCategoria = ordenes
                .SelectMany(o => o.Detalles)
                .Where(d => d.Producto.Categoria != null)
                .GroupBy(d => d.Producto.Categoria.Nombre)
                .Select(g => new
                {
                    Categoria = g.Key,
                    Total = g.Sum(d => d.Subtotal),
                    Cantidad = g.Sum(d => d.Cantidad)
                })
                .OrderByDescending(c => c.Total)
                .ToList();

            ViewBag.TotalVentas = totalVentas;
            ViewBag.TotalOrdenes = totalOrdenes;
            ViewBag.PromedioVenta = promedioVenta;
            ViewBag.VentasPorDia = ventasPorDia;
            ViewBag.ProductosMasVendidos = productosMasVendidos;
            ViewBag.VentasPorCategoria = ventasPorCategoria;

            return View("~/Views/Admin/Reportes/Ventas.cshtml");
        }

        // GET: Reportes/Inventario
        public async Task<IActionResult> Inventario(string busqueda, int? categoriaId, bool soloStockBajo = false)
        {
            ViewData["Busqueda"] = busqueda;
            ViewData["CategoriaId"] = categoriaId;
            ViewData["SoloStockBajo"] = soloStockBajo;

            var productosQuery = _context.Productos
                .Include(p => p.Categoria)
                .Include(p => p.Marca)
                .Include(p => p.Proveedor)
                .Where(p => p.Activo)
                .AsQueryable();

            // Filtros
            if (!string.IsNullOrEmpty(busqueda))
            {
                productosQuery = productosQuery.Where(p =>
                    p.Nombre.Contains(busqueda) ||
                    p.SKU.Contains(busqueda)
                );
            }

            if (categoriaId.HasValue)
            {
                productosQuery = productosQuery.Where(p => p.CategoriaId == categoriaId.Value);
            }

            if (soloStockBajo)
            {
                productosQuery = productosQuery.Where(p => p.Stock <= p.StockMinimo);
            }

            var productos = await productosQuery
                .OrderBy(p => p.Nombre)
                .ToListAsync();

            // Estadísticas generales
            var valorTotalInventario = productos.Sum(p => p.Stock * p.Precio);
            var productosTotales = productos.Count;
            var productosStockBajo = productos.Count(p => p.Stock <= p.StockMinimo);
            var productosSinStock = productos.Count(p => p.Stock == 0);

            ViewBag.ValorTotalInventario = valorTotalInventario;
            ViewBag.ProductosTotales = productosTotales;
            ViewBag.ProductosStockBajo = productosStockBajo;
            ViewBag.ProductosSinStock = productosSinStock;

            // Categorías para el filtro
            ViewBag.Categorias = await _context.Categorias
                .Where(c => c.Activo)
                .OrderBy(c => c.Nombre)
                .ToListAsync();

            return View("~/Views/Admin/Reportes/Inventario.cshtml", productos);
        }

        // GET: Reportes/StockBajo
        public async Task<IActionResult> StockBajo()
        {
            var productosStockBajo = await _context.Productos
                .Include(p => p.Categoria)
                .Include(p => p.Marca)
                .Include(p => p.Proveedor)
                .Where(p => p.Activo && p.Stock <= p.StockMinimo)
                .OrderBy(p => p.Stock)
                .ToListAsync();

            // Agrupar por criticidad
            var productosCriticos = productosStockBajo.Where(p => p.Stock == 0).ToList();
            var productosAlerta = productosStockBajo
                .Where(p => p.Stock > 0 && p.Stock <= (p.StockMinimo * 0.5m))
                .ToList();
            var productosAtencion = productosStockBajo
                .Where(p => p.Stock > (p.StockMinimo * 0.5m) && p.Stock <= p.StockMinimo)
                .ToList();

            ViewBag.ProductosCriticos = productosCriticos;
            ViewBag.ProductosAlerta = productosAlerta;
            ViewBag.ProductosAtencion = productosAtencion;
            ViewBag.TotalCriticos = productosCriticos.Count;
            ViewBag.TotalAlerta = productosAlerta.Count;
            ViewBag.TotalAtencion = productosAtencion.Count;

            // Valor total del inventario en riesgo
            var valorInventarioRiesgo = productosStockBajo.Sum(p => p.Stock * p.Precio);
            ViewBag.ValorInventarioRiesgo = valorInventarioRiesgo;

            return View("~/Views/Admin/Reportes/StockBajo.cshtml", productosStockBajo);
        }

        // API: Exportar reporte de ventas a CSV
        [HttpGet]
        public async Task<IActionResult> ExportarVentasCSV(DateTime? fechaInicio, DateTime? fechaFin)
        {
            try
            {
                // Valores por defecto si no se proporcionan
                fechaInicio ??= DateTime.Today.AddMonths(-1);
                fechaFin ??= DateTime.Today;

                var ordenes = await _context.Ordenes
                    .Include(o => o.Cliente)
                    .Include(o => o.Detalles)
                        .ThenInclude(d => d.Producto)
                    .Where(o => o.FechaOrden >= fechaInicio.Value &&
                               o.FechaOrden <= fechaFin.Value &&
                               o.Estado != "Cancelado")
                    .AsNoTracking()
                    .OrderBy(o => o.FechaOrden)
                    .ToListAsync();

                // Crear CSV en memoria
                using var memoryStream = new System.IO.MemoryStream();
                using var writer = new System.IO.StreamWriter(memoryStream, System.Text.Encoding.UTF8);

                // Header
                await writer.WriteLineAsync("Fecha,NumeroOrden,Cliente,Estado,Subtotal,Descuento,Impuesto,Envio,Total");

                // Datos
                foreach (var orden in ordenes)
                {
                    var cliente = (orden.Cliente?.Email ?? "N/A").Replace("\"", "\"\"");

                    var linea = $"{orden.FechaOrden:yyyy-MM-dd HH:mm}," +
                               $"{orden.NumeroOrden}," +
                               $"\"{cliente}\"," +
                               $"{orden.Estado}," +
                               $"{orden.Subtotal:F2}," +
                               $"{orden.Descuento:F2}," +
                               $"{orden.Impuesto:F2}," +
                               $"{orden.CostoEnvio:F2}," +
                               $"{orden.Total:F2}";

                    await writer.WriteLineAsync(linea);
                }

                await writer.FlushAsync();

                var fileName = $"Ventas_{fechaInicio.Value:yyyyMMdd}_{fechaFin.Value:yyyyMMdd}.csv";
                return File(memoryStream.ToArray(), "text/csv", fileName);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en ExportarVentasCSV: {ex.Message}");
                TempData["Error"] = $"Error al generar CSV: {ex.Message}";
                return RedirectToAction(nameof(Ventas));
            }
        }

        // API: Exportar inventario a CSV
        [HttpGet]
        public async Task<IActionResult> ExportarInventarioCSV(string busqueda, int? categoriaId, bool soloStockBajo = false)
        {
            try
            {
                var productosQuery = _context.Productos
                    .Include(p => p.Categoria)
                    .Include(p => p.Marca)
                    .Include(p => p.Proveedor)
                    .Where(p => p.Activo)
                    .AsNoTracking()
                    .AsQueryable();

                // Aplicar los mismos filtros que en la vista
                if (!string.IsNullOrEmpty(busqueda))
                {
                    productosQuery = productosQuery.Where(p =>
                        p.Nombre.Contains(busqueda) ||
                        p.SKU.Contains(busqueda)
                    );
                }

                if (categoriaId.HasValue)
                {
                    productosQuery = productosQuery.Where(p => p.CategoriaId == categoriaId.Value);
                }

                if (soloStockBajo)
                {
                    productosQuery = productosQuery.Where(p => p.Stock <= p.StockMinimo);
                }

                var productos = await productosQuery
                    .OrderBy(p => p.Nombre)
                    .ToListAsync();

                // Crear CSV en memoria
                using var memoryStream = new System.IO.MemoryStream();
                using var writer = new System.IO.StreamWriter(memoryStream, System.Text.Encoding.UTF8);

                // Header
                await writer.WriteLineAsync("SKU,Nombre,Categoria,Marca,Proveedor,Stock,StockMinimo,Precio,ValorTotal,Estado");

                // Datos
                foreach (var producto in productos)
                {
                    var valorTotal = producto.Stock * producto.Precio;
                    var porcentajeStock = producto.StockMinimo > 0
                        ? ((decimal)producto.Stock / producto.StockMinimo) * 100
                        : 100;

                    var estado = producto.Stock == 0 ? "Sin Stock" :
                                porcentajeStock <= 25 ? "Critico" :
                                porcentajeStock <= 50 ? "Bajo" :
                                porcentajeStock <= 100 ? "Atencion" : "Normal";

                    var nombre = producto.Nombre.Replace("\"", "\"\"");
                    var categoria = (producto.Categoria?.Nombre ?? "N/A").Replace("\"", "\"\"");
                    var marca = (producto.Marca?.Nombre ?? "N/A").Replace("\"", "\"\"");
                    var proveedor = (producto.Proveedor?.Nombre ?? "N/A").Replace("\"", "\"\"");

                    var linea = $"{producto.SKU}," +
                               $"\"{nombre}\"," +
                               $"\"{categoria}\"," +
                               $"\"{marca}\"," +
                               $"\"{proveedor}\"," +
                               $"{producto.Stock}," +
                               $"{producto.StockMinimo}," +
                               $"{producto.Precio:F2}," +
                               $"{valorTotal:F2}," +
                               $"{estado}";

                    await writer.WriteLineAsync(linea);
                }

                await writer.FlushAsync();

                var fileName = $"Inventario_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
                return File(memoryStream.ToArray(), "text/csv", fileName);
            }
            catch (Exception ex)
            {
                // Log del error
                System.Diagnostics.Debug.WriteLine($"Error en ExportarInventarioCSV: {ex.Message}");
                TempData["Error"] = $"Error al generar CSV: {ex.Message}";
                return RedirectToAction(nameof(Inventario));
            }
        }
    }
}