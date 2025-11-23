using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using EcommerceSystem.Data;
using EcommerceSystem.Models.DTOs;
using EcommerceSystem.Models.Entities;
using EcommerceSystem.Services.Interfaces;
using System.Security.Claims;

namespace EcommerceSystem.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrdenesApiController : ControllerBase
    {
        private readonly IOrdenService _ordenService;
        private readonly IProductoService _productoService;
        private readonly ApplicationDbContext _context;

        public OrdenesApiController(
            IOrdenService ordenService,
            IProductoService productoService,
            ApplicationDbContext context)
        {
            _ordenService = ordenService;
            _productoService = productoService;
            _context = context;
        }

        /// <summary>
        /// Crear nueva orden desde app móvil
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<OrdenResponseDto>> CrearOrden([FromBody] CrearOrdenDto model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (model.Items == null || !model.Items.Any())
                return BadRequest(new { message = "La orden debe contener al menos un producto" });

            // Verificar disponibilidad de todos los productos
            var productosNoDisponibles = new List<string>();
            var detalles = new List<OrdenDetalle>();
            decimal subtotal = 0;

            foreach (var item in model.Items)
            {
                var producto = await _productoService.ObtenerPorIdAsync(item.ProductoId);

                if (producto == null)
                {
                    productosNoDisponibles.Add($"Producto ID {item.ProductoId} no encontrado");
                    continue;
                }

                if (producto.Stock < item.Cantidad)
                {
                    productosNoDisponibles.Add($"{producto.Nombre}: stock insuficiente (disponible: {producto.Stock}, solicitado: {item.Cantidad})");
                    continue;
                }

                detalles.Add(new OrdenDetalle
                {
                    ProductoId = producto.Id,
                    Cantidad = item.Cantidad,
                    PrecioUnitario = producto.Precio,
                    Subtotal = producto.Precio * item.Cantidad
                });

                subtotal += producto.Precio * item.Cantidad;
            }

            if (productosNoDisponibles.Any())
            {
                return BadRequest(new
                {
                    message = "Algunos productos no están disponibles",
                    errores = productosNoDisponibles
                });
            }

            // Verificar o crear cliente
            var cliente = await _context.Clientes.FirstOrDefaultAsync(c => c.Id == model.ClienteId);
            if (cliente == null)
            {
                return BadRequest(new { message = $"Cliente con ID {model.ClienteId} no encontrado" });
            }

            // Crear la orden
            var orden = new Orden
            {
                NumeroOrden = await _ordenService.GenerarNumeroOrdenAsync(),
                ClienteId = model.ClienteId,
                DireccionEnvio = model.DireccionEnvio ?? cliente.Direccion,
                NotasCliente = model.NotasCliente,
                Subtotal = subtotal,
                Total = subtotal, // Sin impuestos ni envío por ahora
                Estado = "Pendiente",
                Detalles = detalles
            };

            var (success, message, ordenCreada) = await _ordenService.CrearOrdenAsync(orden);

            if (!success || ordenCreada == null)
            {
                return BadRequest(new { message = message ?? "Error al crear la orden" });
            }

            return CreatedAtAction(nameof(GetOrden), new { id = ordenCreada.Id }, new OrdenResponseDto
            {
                Id = ordenCreada.Id,
                NumeroOrden = ordenCreada.NumeroOrden,
                Estado = ordenCreada.Estado,
                Total = ordenCreada.Total,
                FechaOrden = ordenCreada.FechaOrden
            });
        }

        /// <summary>
        /// Obtener orden por ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<OrdenDetalladaDto>> GetOrden(int id)
        {
            var orden = await _ordenService.ObtenerPorIdAsync(id);

            if (orden == null)
                return NotFound(new { message = $"Orden con ID {id} no encontrada" });

            return Ok(MapToDetalladaDto(orden));
        }

        /// <summary>
        /// Consultar estado de orden por número de orden
        /// </summary>
        [HttpGet("estado/{numeroOrden}")]
        public async Task<ActionResult> GetEstadoOrden(string numeroOrden)
        {
            var orden = await _context.Ordenes
                .Include(o => o.Cliente)
                .FirstOrDefaultAsync(o => o.NumeroOrden == numeroOrden);

            if (orden == null)
                return NotFound(new { message = $"Orden {numeroOrden} no encontrada" });

            return Ok(new
            {
                numeroOrden = orden.NumeroOrden,
                estado = orden.Estado,
                fechaOrden = orden.FechaOrden,
                fechaEnvio = orden.FechaEnvio,
                fechaEntrega = orden.FechaEntrega,
                numeroSeguimiento = orden.NumeroSeguimiento,
                cliente = orden.Cliente?.NombreCompleto,
                total = orden.Total
            });
        }

        /// <summary>
        /// Obtener órdenes de un cliente
        /// </summary>
        [HttpGet("cliente/{clienteId}")]
        public async Task<ActionResult<IEnumerable<OrdenResponseDto>>> GetOrdenesCliente(int clienteId)
        {
            var ordenes = await _ordenService.ObtenerPorClienteAsync(clienteId);
            var result = ordenes.Select(o => new OrdenResponseDto
            {
                Id = o.Id,
                NumeroOrden = o.NumeroOrden,
                Estado = o.Estado,
                Total = o.Total,
                FechaOrden = o.FechaOrden
            });

            return Ok(result);
        }

        /// <summary>
        /// Cambiar estado de orden (requiere autenticación Admin/Vendedor)
        /// </summary>
        [HttpPatch("{id}/estado")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin,Vendedor")]
        public async Task<ActionResult> CambiarEstado(int id, [FromBody] CambiarEstadoDto model)
        {
            var estadosValidos = new[] { "Pendiente", "Confirmado", "Enviado", "Entregado", "Cancelado" };

            if (!estadosValidos.Contains(model.NuevoEstado))
            {
                return BadRequest(new
                {
                    message = "Estado inválido",
                    estadosValidos = estadosValidos
                });
            }

            var exito = await _ordenService.CambiarEstadoAsync(id, model.NuevoEstado);

            if (!exito)
                return NotFound(new { message = $"Orden con ID {id} no encontrada o no se pudo cambiar el estado" });

            return Ok(new { message = $"Estado cambiado a '{model.NuevoEstado}' correctamente" });
        }

        /// <summary>
        /// Cancelar orden
        /// </summary>
        [HttpPost("{id}/cancelar")]
        public async Task<ActionResult> CancelarOrden(int id)
        {
            var orden = await _ordenService.ObtenerPorIdAsync(id);

            if (orden == null)
                return NotFound(new { message = $"Orden con ID {id} no encontrada" });

            if (orden.Estado == "Enviado" || orden.Estado == "Entregado")
            {
                return BadRequest(new { message = "No se puede cancelar una orden que ya fue enviada o entregada" });
            }

            var exito = await _ordenService.CancelarOrdenAsync(id);

            if (!exito)
                return BadRequest(new { message = "Error al cancelar la orden" });

            return Ok(new { message = "Orden cancelada correctamente. El stock ha sido restaurado." });
        }

        /// <summary>
        /// Listar todas las órdenes (requiere autenticación Admin/Vendedor)
        /// </summary>
        [HttpGet]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin,Vendedor")]
        public async Task<ActionResult<IEnumerable<OrdenResponseDto>>> GetOrdenes(
            [FromQuery] string? estado = null,
            [FromQuery] DateTime? desde = null,
            [FromQuery] DateTime? hasta = null)
        {
            var query = _context.Ordenes
                .Include(o => o.Cliente)
                .AsQueryable();

            if (!string.IsNullOrEmpty(estado))
                query = query.Where(o => o.Estado == estado);

            if (desde.HasValue)
                query = query.Where(o => o.FechaOrden >= desde.Value);

            if (hasta.HasValue)
                query = query.Where(o => o.FechaOrden <= hasta.Value);

            var ordenes = await query
                .OrderByDescending(o => o.FechaOrden)
                .ToListAsync();

            var result = ordenes.Select(o => new OrdenResponseDto
            {
                Id = o.Id,
                NumeroOrden = o.NumeroOrden,
                Estado = o.Estado,
                Total = o.Total,
                FechaOrden = o.FechaOrden
            });

            return Ok(result);
        }

        private static OrdenDetalladaDto MapToDetalladaDto(Orden o)
        {
            return new OrdenDetalladaDto
            {
                Id = o.Id,
                NumeroOrden = o.NumeroOrden,
                Estado = o.Estado,
                Subtotal = o.Subtotal,
                Descuento = o.Descuento,
                Impuesto = o.Impuesto,
                CostoEnvio = o.CostoEnvio,
                Total = o.Total,
                FechaOrden = o.FechaOrden,
                FechaPago = o.FechaPago,
                FechaEnvio = o.FechaEnvio,
                FechaEntrega = o.FechaEntrega,
                DireccionEnvio = o.DireccionEnvio,
                NumeroSeguimiento = o.NumeroSeguimiento,
                NotasCliente = o.NotasCliente,
                Cliente = o.Cliente != null ? new ClienteDto
                {
                    Id = o.Cliente.Id,
                    NombreCompleto = o.Cliente.NombreCompleto,
                    Email = o.Cliente.Email,
                    Telefono = o.Cliente.Telefono
                } : null,
                Items = o.Detalles?.Select(d => new OrdenItemDetalleDto
                {
                    ProductoId = d.ProductoId,
                    ProductoNombre = d.Producto?.Nombre ?? "N/A",
                    Cantidad = d.Cantidad,
                    PrecioUnitario = d.PrecioUnitario,
                    Subtotal = d.Subtotal
                }).ToList() ?? new List<OrdenItemDetalleDto>()
            };
        }
    }

    // DTOs adicionales para la API de órdenes
    public class CambiarEstadoDto
    {
        public string NuevoEstado { get; set; } = string.Empty;
    }

    public class OrdenDetalladaDto
    {
        public int Id { get; set; }
        public string NumeroOrden { get; set; } = string.Empty;
        public string Estado { get; set; } = string.Empty;
        public decimal Subtotal { get; set; }
        public decimal Descuento { get; set; }
        public decimal Impuesto { get; set; }
        public decimal CostoEnvio { get; set; }
        public decimal Total { get; set; }
        public DateTime FechaOrden { get; set; }
        public DateTime? FechaPago { get; set; }
        public DateTime? FechaEnvio { get; set; }
        public DateTime? FechaEntrega { get; set; }
        public string? DireccionEnvio { get; set; }
        public string? NumeroSeguimiento { get; set; }
        public string? NotasCliente { get; set; }
        public ClienteDto? Cliente { get; set; }
        public List<OrdenItemDetalleDto> Items { get; set; } = new();
    }

    public class ClienteDto
    {
        public int Id { get; set; }
        public string NombreCompleto { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Telefono { get; set; }
    }

    public class OrdenItemDetalleDto
    {
        public int ProductoId { get; set; }
        public string ProductoNombre { get; set; } = string.Empty;
        public int Cantidad { get; set; }
        public decimal PrecioUnitario { get; set; }
        public decimal Subtotal { get; set; }
    }
}
