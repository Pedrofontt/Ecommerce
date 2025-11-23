using EcommerceSystem.Models.Entities;

namespace EcommerceSystem.Services.Interfaces
{
    public interface IOrdenService
    {
        Task<(bool Success, string Message, Orden? Orden)> CrearOrdenAsync(Orden orden);
        Task<Orden?> ObtenerPorIdAsync(int id);
        Task<List<Orden>> ObtenerPorClienteAsync(int clienteId);
        Task<bool> CambiarEstadoAsync(int ordenId, string nuevoEstado);
        Task<bool> CancelarOrdenAsync(int ordenId);
        Task<string> GenerarNumeroOrdenAsync();
    }
}