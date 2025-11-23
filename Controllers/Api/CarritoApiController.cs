using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EcommerceSystem.Data;
using EcommerceSystem.Models.Entities;
using EcommerceSystem.Services.Interfaces;

namespace EcommerceSystem.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    public class CarritoApiController : ControllerBase
    {
        private readonly ICarritoService _carritoService;
        private readonly IProductoService _productoService;
        private readonly ApplicationDbContext _context;

        public CarritoApiController(
            ICarritoService carritoService,
            IProductoService productoService,
            ApplicationDbContext context)
        {
            _carritoService = carritoService;
            _productoService = productoService;
            _context = context;
        }

        /// <summary>
        /// Obtener carrito actual por sessionId o usuarioId
        /// </summary>
        [HttpGet]
        public async Task<ActionResult> GetCarrito([FromQuery] string? sessionId, [FromQuery] string? usuarioId)
        {
            if (string.IsNullOrEmpty(sessionId) && string.IsNullOrEmpty(usuarioId))
            {
                return BadRequest(new { message = "Debe proporcionar sessionId o usuarioId" });
            }

            var carrito = await _carritoService.ObtenerCarritoActualAsync(usuarioId, sessionId);

            if (carrito == null)
            {
                return Ok(new
                {
                    items = new List<object>(),
                    totalItems = 0,
                    subtotal = 0m
                });
            }

            var items = carrito.Items?.Select(i => new
            {
                id = i.Id,
                productoId = i.ProductoId,
                productoNombre = i.Producto?.Nombre,
                productoSKU = i.Producto?.SKU,
                precioUnitario = i.PrecioUnitario,
                cantidad = i.Cantidad,
                subtotal = i.PrecioUnitario * i.Cantidad,
                imagenUrl = i.Producto?.ImagenPrincipal
            }).ToList();

            return Ok(new
            {
                carritoId = carrito.Id,
                items = items,
                totalItems = items?.Sum(i => i.cantidad) ?? 0,
                subtotal = items?.Sum(i => i.subtotal) ?? 0m
            });
        }

        /// <summary>
        /// Agregar producto al carrito
        /// </summary>
        [HttpPost("agregar")]
        public async Task<ActionResult> AgregarItem([FromBody] AgregarItemDto model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Verificar que el producto existe y tiene stock
            var producto = await _productoService.ObtenerPorIdAsync(model.ProductoId);
            if (producto == null)
                return NotFound(new { message = "Producto no encontrado" });

            if (producto.Stock < model.Cantidad)
            {
                return BadRequest(new
                {
                    message = "Stock insuficiente",
                    stockDisponible = producto.Stock
                });
            }

            // Obtener o crear carrito
            var carrito = await _carritoService.ObtenerCarritoActualAsync(model.UsuarioId, model.SessionId);

            if (carrito == null)
            {
                // Crear nuevo carrito
                carrito = new Carrito
                {
                    UsuarioId = model.UsuarioId,
                    SessionId = model.SessionId ?? Guid.NewGuid().ToString(),
                    FechaCreacion = DateTime.Now,
                    Items = new List<CarritoItem>()
                };
                _context.Carritos.Add(carrito);
                await _context.SaveChangesAsync();
            }

            var exito = await _carritoService.AgregarItemAsync(carrito.Id, model.ProductoId, model.Cantidad);

            if (!exito)
                return BadRequest(new { message = "Error al agregar producto al carrito" });

            // Retornar carrito actualizado
            var carritoActualizado = await _carritoService.ObtenerCarritoActualAsync(model.UsuarioId, model.SessionId);
            var items = carritoActualizado?.Items?.Select(i => new
            {
                id = i.Id,
                productoId = i.ProductoId,
                productoNombre = i.Producto?.Nombre,
                cantidad = i.Cantidad,
                subtotal = i.PrecioUnitario * i.Cantidad
            }).ToList();

            return Ok(new
            {
                message = "Producto agregado al carrito",
                carritoId = carritoActualizado?.Id,
                totalItems = items?.Sum(i => i.cantidad) ?? 0,
                subtotal = items?.Sum(i => i.subtotal) ?? 0m
            });
        }

        /// <summary>
        /// Actualizar cantidad de un item
        /// </summary>
        [HttpPut("item/{itemId}")]
        public async Task<ActionResult> ActualizarCantidad(int itemId, [FromBody] ActualizarCantidadDto model)
        {
            if (model.Cantidad <= 0)
                return BadRequest(new { message = "La cantidad debe ser mayor a 0" });

            // Verificar stock disponible
            var item = await _context.CarritoItems
                .Include(i => i.Producto)
                .FirstOrDefaultAsync(i => i.Id == itemId);

            if (item == null)
                return NotFound(new { message = "Item no encontrado en el carrito" });

            if (item.Producto != null && item.Producto.Stock < model.Cantidad)
            {
                return BadRequest(new
                {
                    message = "Stock insuficiente",
                    stockDisponible = item.Producto.Stock
                });
            }

            var exito = await _carritoService.ActualizarCantidadAsync(itemId, model.Cantidad);

            if (!exito)
                return BadRequest(new { message = "Error al actualizar cantidad" });

            return Ok(new { message = "Cantidad actualizada correctamente" });
        }

        /// <summary>
        /// Eliminar item del carrito
        /// </summary>
        [HttpDelete("item/{itemId}")]
        public async Task<ActionResult> EliminarItem(int itemId)
        {
            var exito = await _carritoService.EliminarItemAsync(itemId);

            if (!exito)
                return NotFound(new { message = "Item no encontrado" });

            return Ok(new { message = "Item eliminado del carrito" });
        }

        /// <summary>
        /// Vaciar carrito
        /// </summary>
        [HttpDelete("{carritoId}")]
        public async Task<ActionResult> VaciarCarrito(int carritoId)
        {
            var exito = await _carritoService.VaciarCarritoAsync(carritoId);

            if (!exito)
                return NotFound(new { message = "Carrito no encontrado" });

            return Ok(new { message = "Carrito vaciado correctamente" });
        }
    }

    public class AgregarItemDto
    {
        public int ProductoId { get; set; }
        public int Cantidad { get; set; } = 1;
        public string? UsuarioId { get; set; }
        public string? SessionId { get; set; }
    }

    public class ActualizarCantidadDto
    {
        public int Cantidad { get; set; }
    }
}
