using System.ComponentModel.DataAnnotations;

namespace EcommerceSystem.Models.Entities
{
	public class Cliente
	{
		public int Id { get; set; }

		[Required, MaxLength(150)]
		public string NombreCompleto { get; set; }

		[Required, MaxLength(150), EmailAddress]
		public string Email { get; set; }

		[MaxLength(20)]
		public string? Telefono { get; set; }

		[MaxLength(200)]
		public string? Direccion { get; set; }

		public DateTime FechaRegistro { get; set; } = DateTime.Now;

		// Relación con Identity User
		public string? UsuarioId { get; set; }

		// Navegación
		public virtual ICollection<Orden> Ordenes { get; set; } = new List<Orden>();
	}
}