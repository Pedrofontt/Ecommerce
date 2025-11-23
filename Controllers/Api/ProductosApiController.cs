using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using EcommerceSystem.Models.DTOs;
using EcommerceSystem.Models.Entities;
using EcommerceSystem.Services.Interfaces;

namespace EcommerceSystem.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductosApiController : ControllerBase
    {
        private readonly IProductoService _productoService;
        private readonly ICategoriaService _categoriaService;

        public ProductosApiController(
            IProductoService productoService,
            ICategoriaService categoriaService)
        {
            _productoService = productoService;
            _categoriaService = categoriaService;
        }

        /// <summary>
        /// Obtener todos los productos activos
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ProductoDto>>> GetProductos()
        {
            var productos = await _productoService.ObtenerTodosAsync();
            var result = productos.Select(MapToDto);
            return Ok(result);
        }

        /// <summary>
        /// Obtener producto por ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<ProductoDetalleDto>> GetProducto(int id)
        {
            var producto = await _productoService.ObtenerPorIdAsync(id);

            if (producto == null)
                return NotFound(new { message = $"Producto con ID {id} no encontrado" });

            return Ok(MapToDetalleDto(producto));
        }

        /// <summary>
        /// Obtener producto por SKU
        /// </summary>
        [HttpGet("sku/{sku}")]
        public async Task<ActionResult<ProductoDetalleDto>> GetProductoPorSKU(string sku)
        {
            var producto = await _productoService.ObtenerPorSKUAsync(sku);

            if (producto == null)
                return NotFound(new { message = $"Producto con SKU {sku} no encontrado" });

            var detalle = await _productoService.ObtenerPorIdAsync(producto.Id);
            return Ok(MapToDetalleDto(detalle!));
        }

        /// <summary>
        /// Buscar productos por término
        /// </summary>
        [HttpGet("buscar")]
        public async Task<ActionResult<IEnumerable<ProductoDto>>> BuscarProductos([FromQuery] string q)
        {
            if (string.IsNullOrWhiteSpace(q))
                return BadRequest(new { message = "Debe proporcionar un término de búsqueda" });

            var productos = await _productoService.BuscarAsync(q);
            var result = productos.Select(MapToDto);
            return Ok(result);
        }

        /// <summary>
        /// Obtener productos por categoría
        /// </summary>
        [HttpGet("categoria/{categoriaId}")]
        public async Task<ActionResult<IEnumerable<ProductoDto>>> GetProductosPorCategoria(int categoriaId)
        {
            var productos = await _productoService.ObtenerPorCategoriaAsync(categoriaId);
            var result = productos.Select(MapToDto);
            return Ok(result);
        }

        /// <summary>
        /// Obtener productos destacados
        /// </summary>
        [HttpGet("destacados")]
        public async Task<ActionResult<IEnumerable<ProductoDto>>> GetProductosDestacados()
        {
            var productos = await _productoService.ObtenerDestacadosAsync();
            var result = productos.Select(MapToDto);
            return Ok(result);
        }

        /// <summary>
        /// Verificar disponibilidad de un producto
        /// </summary>
        [HttpGet("{id}/disponibilidad")]
        public async Task<ActionResult> GetDisponibilidad(int id, [FromQuery] int cantidad = 1)
        {
            var producto = await _productoService.ObtenerPorIdAsync(id);

            if (producto == null)
                return NotFound(new { message = $"Producto con ID {id} no encontrado" });

            return Ok(new
            {
                productoId = producto.Id,
                sku = producto.SKU,
                nombre = producto.Nombre,
                stockActual = producto.Stock,
                cantidadSolicitada = cantidad,
                disponible = producto.Stock >= cantidad,
                activo = producto.Activo
            });
        }

        /// <summary>
        /// Verificar disponibilidad de múltiples productos
        /// </summary>
        [HttpPost("disponibilidad")]
        public async Task<ActionResult> VerificarDisponibilidadMultiple([FromBody] List<DisponibilidadRequest> items)
        {
            var resultados = new List<object>();

            foreach (var item in items)
            {
                var producto = await _productoService.ObtenerPorIdAsync(item.ProductoId);

                if (producto == null)
                {
                    resultados.Add(new
                    {
                        productoId = item.ProductoId,
                        encontrado = false,
                        disponible = false
                    });
                }
                else
                {
                    resultados.Add(new
                    {
                        productoId = producto.Id,
                        sku = producto.SKU,
                        nombre = producto.Nombre,
                        encontrado = true,
                        stockActual = producto.Stock,
                        cantidadSolicitada = item.Cantidad,
                        disponible = producto.Stock >= item.Cantidad
                    });
                }
            }

            return Ok(new
            {
                items = resultados,
                todosDisponibles = resultados.All(r => ((dynamic)r).disponible)
            });
        }

        /// <summary>
        /// Obtener productos con stock bajo (requiere autenticación Admin/Vendedor)
        /// </summary>
        [HttpGet("stock-bajo")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin,Vendedor")]
        public async Task<ActionResult<IEnumerable<ProductoDto>>> GetProductosStockBajo()
        {
            var productos = await _productoService.ObtenerStockBajoAsync();
            var result = productos.Select(MapToDto);
            return Ok(result);
        }

        /// <summary>
        /// Crear nuevo producto (requiere autenticación Admin)
        /// </summary>
        [HttpPost]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin")]
        public async Task<ActionResult<ProductoDto>> CrearProducto([FromBody] CrearProductoDto model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var producto = new Producto
            {
                Nombre = model.Nombre,
                Descripcion = model.Descripcion,
                Precio = model.Precio,
                Stock = model.Stock,
                CategoriaId = model.CategoriaId,
                MarcaId = model.MarcaId,
                Activo = true
            };

            var exito = await _productoService.CrearAsync(producto);

            if (!exito)
                return BadRequest(new { message = "Error al crear el producto" });

            var creado = await _productoService.ObtenerPorIdAsync(producto.Id);
            return CreatedAtAction(nameof(GetProducto), new { id = producto.Id }, MapToDto(creado!));
        }

        /// <summary>
        /// Actualizar producto existente (requiere autenticación Admin)
        /// </summary>
        [HttpPut("{id}")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin")]
        public async Task<ActionResult> ActualizarProducto(int id, [FromBody] CrearProductoDto model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var producto = await _productoService.ObtenerPorIdAsync(id);
            if (producto == null)
                return NotFound(new { message = $"Producto con ID {id} no encontrado" });

            producto.Nombre = model.Nombre;
            producto.Descripcion = model.Descripcion;
            producto.Precio = model.Precio;
            producto.Stock = model.Stock;
            producto.CategoriaId = model.CategoriaId;
            producto.MarcaId = model.MarcaId;

            var exito = await _productoService.ActualizarAsync(producto);

            if (!exito)
                return BadRequest(new { message = "Error al actualizar el producto" });

            return Ok(new { message = "Producto actualizado correctamente" });
        }

        /// <summary>
        /// Eliminar producto (soft delete, requiere autenticación Admin)
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin")]
        public async Task<ActionResult> EliminarProducto(int id)
        {
            var exito = await _productoService.EliminarAsync(id);

            if (!exito)
                return NotFound(new { message = $"Producto con ID {id} no encontrado" });

            return Ok(new { message = "Producto eliminado correctamente" });
        }

        /// <summary>
        /// Obtener todas las categorías
        /// </summary>
        [HttpGet("categorias")]
        public async Task<ActionResult> GetCategorias()
        {
            var categorias = await _categoriaService.ObtenerTodasAsync();
            var result = categorias.Select(c => new
            {
                id = c.Id,
                nombre = c.Nombre,
                descripcion = c.Descripcion,
                categoriaPadreId = c.CategoriaPadreId
            });
            return Ok(result);
        }

        // Métodos de mapeo
        private static ProductoDto MapToDto(Producto p)
        {
            return new ProductoDto
            {
                Id = p.Id,
                SKU = p.SKU,
                Nombre = p.Nombre,
                DescripcionCorta = p.DescripcionCorta,
                Precio = p.Precio,
                PrecioComparacion = p.PrecioComparacion,
                Stock = p.Stock,
                ImagenPrincipal = p.ImagenPrincipal,
                Destacado = p.Destacado,
                CategoriaNombre = p.Categoria?.Nombre,
                MarcaNombre = p.Marca?.Nombre
            };
        }

        private static ProductoDetalleDto MapToDetalleDto(Producto p)
        {
            return new ProductoDetalleDto
            {
                Id = p.Id,
                SKU = p.SKU,
                Nombre = p.Nombre,
                DescripcionCorta = p.DescripcionCorta,
                Descripcion = p.Descripcion,
                Precio = p.Precio,
                PrecioComparacion = p.PrecioComparacion,
                Stock = p.Stock,
                ImagenPrincipal = p.ImagenPrincipal,
                Destacado = p.Destacado,
                CategoriaNombre = p.Categoria?.Nombre,
                MarcaNombre = p.Marca?.Nombre,
                Peso = p.Peso,
                Dimensiones = p.Dimensiones,
                Imagenes = p.Imagenes?.Select(i => i.Url).ToList()
            };
        }
    }

    public class DisponibilidadRequest
    {
        public int ProductoId { get; set; }
        public int Cantidad { get; set; } = 1;
    }
}
