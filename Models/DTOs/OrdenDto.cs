namespace EcommerceSystem.Models.DTOs
{
    public class CrearOrdenDto
    {
        public int ClienteId { get; set; }
        public string? DireccionEnvio { get; set; }
        public string? NotasCliente { get; set; }
        public List<OrdenItemDto> Items { get; set; }
    }

    public class OrdenItemDto
    {
        public int ProductoId { get; set; }
        public int Cantidad { get; set; }
    }

    public class OrdenResponseDto
    {
        public int Id { get; set; }
        public string NumeroOrden { get; set; }
        public string Estado { get; set; }
        public decimal Total { get; set; }
        public DateTime FechaOrden { get; set; }
    }
}