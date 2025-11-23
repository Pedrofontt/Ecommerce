using EcommerceSystem.Models.Entities;

namespace EcommerceSystem.Services.Interfaces
{
    public interface IKardexService
    {
        Task RegistrarMovimientoAsync(int productoId, string tipo, int cantidad, string referencia, string? descripcion = null);
        Task<List<Kardex>> ObtenerPorProductoAsync(int productoId);
        Task<List<Kardex>> ObtenerPorFechaAsync(DateTime fechaInicio, DateTime fechaFin);
    }
}