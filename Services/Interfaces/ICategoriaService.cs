using EcommerceSystem.Models.Entities;

namespace EcommerceSystem.Services.Interfaces
{
    public interface ICategoriaService
    {
        Task<List<Categoria>> ObtenerTodasAsync();
        Task<Categoria?> ObtenerPorIdAsync(int id);
        Task<bool> CrearAsync(Categoria categoria);
        Task<bool> ActualizarAsync(Categoria categoria);
        Task<bool> EliminarAsync(int id);
    }
}