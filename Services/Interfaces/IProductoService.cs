using EcommerceSystem.Models.Entities;
using EcommerceSystem.Models.ViewModels;

namespace EcommerceSystem.Services.Interfaces
{
    public interface IProductoService
    {
        Task<List<Producto>> ObtenerTodosAsync();
        Task<Producto?> ObtenerPorIdAsync(int id);
        Task<Producto?> ObtenerPorSKUAsync(string sku);
        Task<List<Producto>> BuscarAsync(string termino);
        Task<List<Producto>> ObtenerPorCategoriaAsync(int categoriaId);
        Task<List<Producto>> ObtenerDestacadosAsync();
        Task<bool> CrearAsync(Producto producto);
        Task<bool> ActualizarAsync(Producto producto);
        Task<bool> EliminarAsync(int id);
        Task<bool> ExisteSKUAsync(string sku, int? productoId = null);
        Task<List<Producto>> ObtenerStockBajoAsync();
    }
}