using EcommerceSystem.Models.Entities;

namespace EcommerceSystem.Services.Interfaces
{
    public interface IAlertaService
    {
        Task VerificarStockYCrearAlertasAsync(int productoId);
        Task<List<AlertaStock>> ObtenerAlertasPendientesAsync();
        Task<bool> MarcarComoRevisadaAsync(int alertaId, string usuarioId);
    }
}