using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using EcommerceSystem.Models.Entities;

namespace EcommerceSystem.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // DbSets - SOLO UNA DECLARACIÓN DE CADA UNO
        public DbSet<Cliente> Clientes { get; set; }
        public DbSet<Categoria> Categorias { get; set; }
        public DbSet<Marca> Marcas { get; set; }
        public DbSet<Proveedor> Proveedores { get; set; }
        public DbSet<Producto> Productos { get; set; }
        public DbSet<ProductoCategoria> ProductoCategorias { get; set; }
        public DbSet<ImagenProducto> ImagenesProducto { get; set; }
        public DbSet<Orden> Ordenes { get; set; }
        public DbSet<OrdenDetalle> OrdenDetalles { get; set; }
        public DbSet<Pago> Pagos { get; set; }
        public DbSet<Kardex> Kardex { get; set; }
        public DbSet<AlertaStock> AlertasStock { get; set; }
        public DbSet<Carrito> Carritos { get; set; }
        public DbSet<CarritoItem> CarritoItems { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Categoria - Jerarquía recursiva
            modelBuilder.Entity<Categoria>()
                .HasOne(c => c.Parent)
                .WithMany(c => c.Subcategorias)
                .HasForeignKey(c => c.ParentId)
                .OnDelete(DeleteBehavior.Restrict);

            // ProductoCategoria - Relación N:M
            modelBuilder.Entity<ProductoCategoria>()
                .HasKey(pc => new { pc.ProductoId, pc.CategoriaId });

            modelBuilder.Entity<ProductoCategoria>()
                .HasOne(pc => pc.Producto)
                .WithMany(p => p.ProductoCategorias)
                .HasForeignKey(pc => pc.ProductoId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ProductoCategoria>()
                .HasOne(pc => pc.Categoria)
                .WithMany(c => c.ProductoCategorias)
                .HasForeignKey(pc => pc.CategoriaId)
                .OnDelete(DeleteBehavior.Cascade);

            // Producto - Categoria (principal)
            modelBuilder.Entity<Producto>()
                .HasOne(p => p.Categoria)
                .WithMany()
                .HasForeignKey(p => p.CategoriaId)
                .OnDelete(DeleteBehavior.SetNull);

            // Producto - Marca
            modelBuilder.Entity<Producto>()
                .HasOne(p => p.Marca)
                .WithMany(m => m.Productos)
                .HasForeignKey(p => p.MarcaId)
                .OnDelete(DeleteBehavior.SetNull);

            // Producto - Proveedor
            modelBuilder.Entity<Producto>()
                .HasOne(p => p.Proveedor)
                .WithMany(pr => pr.Productos)
                .HasForeignKey(p => p.ProveedorId)
                .OnDelete(DeleteBehavior.SetNull);

            // ImagenProducto - Producto
            modelBuilder.Entity<ImagenProducto>()
                .HasOne(i => i.Producto)
                .WithMany(p => p.Imagenes)
                .HasForeignKey(i => i.ProductoId)
                .OnDelete(DeleteBehavior.Cascade);

            // Orden - Cliente
            modelBuilder.Entity<Orden>()
                .HasOne(o => o.Cliente)
                .WithMany(c => c.Ordenes)
                .HasForeignKey(o => o.ClienteId)
                .OnDelete(DeleteBehavior.Restrict);

            // OrdenDetalle - Orden
            modelBuilder.Entity<OrdenDetalle>()
                .HasOne(od => od.Orden)
                .WithMany(o => o.Detalles)
                .HasForeignKey(od => od.OrdenId)
                .OnDelete(DeleteBehavior.Cascade);

            // OrdenDetalle - Producto
            modelBuilder.Entity<OrdenDetalle>()
                .HasOne(od => od.Producto)
                .WithMany()
                .HasForeignKey(od => od.ProductoId)
                .OnDelete(DeleteBehavior.Restrict);

            // Pago - Orden
            modelBuilder.Entity<Pago>()
                .HasOne(p => p.Orden)
                .WithMany(o => o.Pagos)
                .HasForeignKey(p => p.OrdenId)
                .OnDelete(DeleteBehavior.Cascade);

            // Kardex - Producto
            modelBuilder.Entity<Kardex>()
                .HasOne(k => k.Producto)
                .WithMany(p => p.MovimientosKardex)
                .HasForeignKey(k => k.ProductoId)
                .OnDelete(DeleteBehavior.Cascade);

            // AlertaStock - Producto
            modelBuilder.Entity<AlertaStock>()
                .HasOne(a => a.Producto)
                .WithMany()
                .HasForeignKey(a => a.ProductoId)
                .OnDelete(DeleteBehavior.Cascade);

            // CarritoItem - Carrito
            modelBuilder.Entity<CarritoItem>()
                .HasOne(ci => ci.Carrito)
                .WithMany(c => c.Items)
                .HasForeignKey(ci => ci.CarritoId)
                .OnDelete(DeleteBehavior.Cascade);

            // CarritoItem - Producto
            modelBuilder.Entity<CarritoItem>()
                .HasOne(ci => ci.Producto)
                .WithMany()
                .HasForeignKey(ci => ci.ProductoId)
                .OnDelete(DeleteBehavior.Restrict);

            // Índices
            modelBuilder.Entity<Producto>()
                .HasIndex(p => p.SKU)
                .IsUnique();

            modelBuilder.Entity<Producto>()
                .HasIndex(p => p.Nombre);

            modelBuilder.Entity<Cliente>()
                .HasIndex(c => c.Email)
                .IsUnique();

            modelBuilder.Entity<Orden>()
                .HasIndex(o => o.NumeroOrden)
                .IsUnique();

            modelBuilder.Entity<Orden>()
                .HasIndex(o => o.Estado);

            // Constraints
            modelBuilder.Entity<Producto>()
                .HasCheckConstraint("CK_Producto_Precio", "[Precio] >= 0");

            modelBuilder.Entity<Producto>()
                .HasCheckConstraint("CK_Producto_Stock", "[Stock] >= 0");

            modelBuilder.Entity<OrdenDetalle>()
                .HasCheckConstraint("CK_OrdenDetalle_Cantidad", "[Cantidad] > 0");

            modelBuilder.Entity<CarritoItem>()
                .HasCheckConstraint("CK_CarritoItem_Cantidad", "[Cantidad] > 0");
        }
    }
}