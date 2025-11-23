using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EcommerceSystem.Data;
using EcommerceSystem.Models.Entities;
using EcommerceSystem.Services.Interfaces;
using System.Security.Claims;

namespace EcommerceSystem.Controllers
{
    public class CarritoController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ICarritoService _carritoService;
        private readonly IOrdenService _ordenService; // ✅ AGREGADO

        public CarritoController(
            ApplicationDbContext context,
            ICarritoService carritoService,
            IOrdenService ordenService) // ✅ AGREGADO
        {
            _context = context;
            _carritoService = carritoService;
            _ordenService = ordenService; // ✅ AGREGADO
        }

        // GET: Carrito
        public async Task<IActionResult> Index()
        {
            var carrito = await ObtenerOCrearCarritoAsync();

            var carritoCompleto = await _context.Carritos
                .Include(c => c.Items)
                    .ThenInclude(i => i.Producto)
                .FirstOrDefaultAsync(c => c.Id == carrito.Id);

            return View(carritoCompleto);
        }

        // POST: Carrito/Agregar
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Agregar(int productoId, int cantidad = 1)
        {
            var producto = await _context.Productos.FindAsync(productoId);

            if (producto == null || !producto.Activo || producto.Stock < cantidad)
            {
                TempData["Error"] = "Producto no disponible o stock insuficiente";
                return RedirectToAction("Producto", "Tienda", new { id = productoId });
            }

            var carrito = await ObtenerOCrearCarritoAsync();

            var resultado = await _carritoService.AgregarItemAsync(carrito.Id, productoId, cantidad);

            if (resultado)
            {
                TempData["Success"] = $"{producto.Nombre} agregado al carrito";
            }
            else
            {
                TempData["Error"] = "Error al agregar producto al carrito";
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: Carrito/ActualizarCantidad
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ActualizarCantidad(int itemId, int cantidad)
        {
            if (cantidad <= 0)
            {
                return await Eliminar(itemId);
            }

            var item = await _context.CarritoItems
                .Include(i => i.Producto)
                .FirstOrDefaultAsync(i => i.Id == itemId);

            if (item == null)
                return NotFound();

            if (item.Producto.Stock < cantidad)
            {
                TempData["Error"] = $"Stock disponible: {item.Producto.Stock}";
                return RedirectToAction(nameof(Index));
            }

            var resultado = await _carritoService.ActualizarCantidadAsync(itemId, cantidad);

            if (resultado)
            {
                TempData["Success"] = "Cantidad actualizada";
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: Carrito/Eliminar
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Eliminar(int itemId)
        {
            var resultado = await _carritoService.EliminarItemAsync(itemId);

            if (resultado)
            {
                TempData["Success"] = "Producto eliminado del carrito";
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: Carrito/Vaciar
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Vaciar()
        {
            var carrito = await ObtenerOCrearCarritoAsync();
            var resultado = await _carritoService.VaciarCarritoAsync(carrito.Id);

            if (resultado)
            {
                TempData["Success"] = "Carrito vaciado";
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: Carrito/Checkout
        public async Task<IActionResult> Checkout()
        {
            var carrito = await ObtenerOCrearCarritoAsync();

            var carritoCompleto = await _context.Carritos
                .Include(c => c.Items)
                    .ThenInclude(i => i.Producto)
                .FirstOrDefaultAsync(c => c.Id == carrito.Id);

            if (carritoCompleto == null || !carritoCompleto.Items.Any())
            {
                TempData["Error"] = "El carrito está vacío";
                return RedirectToAction(nameof(Index));
            }

            return View(carritoCompleto);
        }

        // POST: Carrito/ConfirmarOrden
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmarOrden(
            string nombreCompleto,
            string email,
            string telefono,
            string direccion,
            string notas)
        {
            var carritoActual = await ObtenerOCrearCarritoAsync();

            var carrito = await _context.Carritos
                .Include(c => c.Items)
                    .ThenInclude(i => i.Producto)
                .FirstOrDefaultAsync(c => c.Id == carritoActual.Id);

            if (carrito == null || !carrito.Items.Any())
            {
                TempData["Error"] = "El carrito está vacío";
                return RedirectToAction(nameof(Index));
            }

            // Crear o buscar cliente
            var cliente = await _context.Clientes
                .FirstOrDefaultAsync(c => c.Email == email);

            if (cliente == null)
            {
                cliente = new Cliente
                {
                    NombreCompleto = nombreCompleto,
                    Email = email,
                    Telefono = telefono,
                    Direccion = direccion
                };
                _context.Clientes.Add(cliente);
                await _context.SaveChangesAsync();
            }

            // Crear orden
            var orden = new Orden
            {
                ClienteId = cliente.Id,
                DireccionEnvio = direccion,
                NotasCliente = notas,
                Estado = "Pendiente"
            };

            // Agregar detalles
            foreach (var item in carrito.Items)
            {
                orden.Detalles.Add(new OrdenDetalle
                {
                    ProductoId = item.ProductoId,
                    Cantidad = item.Cantidad,
                    PrecioUnitario = item.Producto.Precio,
                    Subtotal = item.Cantidad * item.Producto.Precio
                });
            }

            // ✅ CORRECCIÓN: Usar el servicio inyectado
            var (success, message, ordenCreada) = await _ordenService.CrearOrdenAsync(orden);

            if (success && ordenCreada != null)
            {
                // Vaciar carrito
                await _carritoService.VaciarCarritoAsync(carrito.Id);

                TempData["Success"] = $"¡Orden #{ordenCreada.NumeroOrden} creada exitosamente!";
                return RedirectToAction(nameof(OrdenExitosa), new { id = ordenCreada.Id });
            }

            TempData["Error"] = message;
            return RedirectToAction(nameof(Checkout));
        }

        // GET: Carrito/OrdenExitosa/5
        public async Task<IActionResult> OrdenExitosa(int? id)
        {
            if (id == null)
                return NotFound();

            var orden = await _context.Ordenes
                .Include(o => o.Cliente)
                .Include(o => o.Detalles)
                    .ThenInclude(d => d.Producto)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (orden == null)
                return NotFound();

            return View(orden);
        }

        // Métodos auxiliares
        private async Task<Carrito> ObtenerOCrearCarritoAsync()
        {
            var usuarioId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var sessionId = HttpContext.Session.Id;

            var carrito = await _carritoService.ObtenerCarritoActualAsync(usuarioId, sessionId);

            if (carrito == null)
            {
                carrito = new Carrito
                {
                    UsuarioId = usuarioId,
                    SessionId = string.IsNullOrEmpty(usuarioId) ? sessionId : null
                };

                _context.Carritos.Add(carrito);
                await _context.SaveChangesAsync();
            }

            return carrito;
        }
    }
}