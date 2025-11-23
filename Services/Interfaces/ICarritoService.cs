using EcommerceSystem.Models.Entities;

namespace EcommerceSystem.Services.Interfaces
{
    public interface ICarritoService
    {
        Task<Carrito?> ObtenerCarritoActualAsync(string? usuarioId, string? sessionId);
        Task<bool> AgregarItemAsync(int carritoId, int productoId, int cantidad);
        Task<bool> ActualizarCantidadAsync(int itemId, int cantidad);
        Task<bool> EliminarItemAsync(int itemId);
        Task<bool> VaciarCarritoAsync(int carritoId);
    }
}