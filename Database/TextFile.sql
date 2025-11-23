-- ==============================================
-- SCRIPT COMPLETO DE BASE DE DATOS
-- Sistema Ecommerce Multiplataforma
-- ==============================================

USE master;
GO

-- Crear base de datos
IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = 'EcommerceDB')
BEGIN
    CREATE DATABASE EcommerceDB;
END
GO

USE EcommerceDB;
GO

-- ==============================================
-- TABLAS DE IDENTIDAD (ASP.NET Identity)
-- ==============================================
-- Nota: Estas serán creadas automáticamente por Entity Framework
-- mediante migraciones. Este script es solo referencial.

-- ==============================================
-- TABLAS DEL SISTEMA
-- ==============================================

-- CLIENTES
CREATE TABLE Clientes (
    Id INT PRIMARY KEY IDENTITY(1,1),
    NombreCompleto NVARCHAR(150) NOT NULL,
    Email NVARCHAR(150) NOT NULL UNIQUE,
    Telefono NVARCHAR(20),
    Direccion NVARCHAR(200),
    FechaRegistro DATETIME2 NOT NULL DEFAULT GETDATE(),
    UsuarioId NVARCHAR(450) NULL, -- FK a AspNetUsers
    INDEX IX_Clientes_Email (Email),
    INDEX IX_Clientes_UsuarioId (UsuarioId)
);

-- CATEGORÍAS (con soporte de jerarquía)
CREATE TABLE Categorias (
    Id INT PRIMARY KEY IDENTITY(1,1),
    Nombre NVARCHAR(150) NOT NULL,
    Descripcion NVARCHAR(500),
    ParentId INT NULL,
    Icono NVARCHAR(50),
    Orden INT DEFAULT 0,
    Activo BIT DEFAULT 1,
    CONSTRAINT FK_Categorias_Parent FOREIGN KEY (ParentId) 
        REFERENCES Categorias(Id),
    INDEX IX_Categorias_ParentId (ParentId),
    INDEX IX_Categorias_Activo (Activo)
);

-- MARCAS
CREATE TABLE Marcas (
    Id INT PRIMARY KEY IDENTITY(1,1),
    Nombre NVARCHAR(100) NOT NULL,
    Descripcion NVARCHAR(300),
    Logo NVARCHAR(255),
    Activo BIT DEFAULT 1,
    INDEX IX_Marcas_Nombre (Nombre)
);

-- PROVEEDORES
CREATE TABLE Proveedores (
    Id INT PRIMARY KEY IDENTITY(1,1),
    Nombre NVARCHAR(150) NOT NULL,
    Contacto NVARCHAR(100),
    Email NVARCHAR(100),
    Telefono NVARCHAR(20),
    Direccion NVARCHAR(200),
    Activo BIT DEFAULT 1,
    INDEX IX_Proveedores_Nombre (Nombre)
);

-- PRODUCTOS
CREATE TABLE Productos (
    Id INT PRIMARY KEY IDENTITY(1,1),
    SKU NVARCHAR(50) NOT NULL UNIQUE,
    Nombre NVARCHAR(150) NOT NULL,
    Descripcion NVARCHAR(MAX),
    DescripcionCorta NVARCHAR(500),
    Precio DECIMAL(18,2) NOT NULL CHECK (Precio >= 0),
    PrecioComparacion DECIMAL(18,2) NULL, -- precio original antes de descuento
    Stock INT NOT NULL DEFAULT 0 CHECK (Stock >= 0),
    StockMinimo INT DEFAULT 5,
    CategoriaId INT NULL,
    MarcaId INT NULL,
    ProveedorId INT NULL,
    Peso DECIMAL(10,2) NULL, -- en kg
    Dimensiones NVARCHAR(50) NULL, -- ej: "30x20x10 cm"
    ImagenPrincipal NVARCHAR(255),
    Destacado BIT DEFAULT 0,
    Activo BIT DEFAULT 1,
    FechaCreacion DATETIME2 NOT NULL DEFAULT GETDATE(),
    FechaModificacion DATETIME2 NULL,
    CONSTRAINT FK_Productos_Categoria FOREIGN KEY (CategoriaId) 
        REFERENCES Categorias(Id) ON DELETE SET NULL,
    CONSTRAINT FK_Productos_Marca FOREIGN KEY (MarcaId) 
        REFERENCES Marcas(Id) ON DELETE SET NULL,
    CONSTRAINT FK_Productos_Proveedor FOREIGN KEY (ProveedorId) 
        REFERENCES Proveedores(Id) ON DELETE SET NULL,
    INDEX IX_Productos_SKU (SKU),
    INDEX IX_Productos_Nombre (Nombre),
    INDEX IX_Productos_Precio (Precio),
    INDEX IX_Productos_CategoriaId (CategoriaId),
    INDEX IX_Productos_MarcaId (MarcaId),
    INDEX IX_Productos_Destacado (Destacado),
    INDEX IX_Productos_Activo (Activo)
);

-- RELACIÓN N:M entre Productos y Categorías
CREATE TABLE ProductoCategorias (
    ProductoId INT NOT NULL,
    CategoriaId INT NOT NULL,
    PRIMARY KEY (ProductoId, CategoriaId),
    CONSTRAINT FK_ProductoCategorias_Producto FOREIGN KEY (ProductoId) 
        REFERENCES Productos(Id) ON DELETE CASCADE,
    CONSTRAINT FK_ProductoCategorias_Categoria FOREIGN KEY (CategoriaId) 
        REFERENCES Categorias(Id) ON DELETE CASCADE
);

-- IMÁGENES DE PRODUCTOS (múltiples imágenes por producto)
CREATE TABLE ImagenesProducto (
    Id INT PRIMARY KEY IDENTITY(1,1),
    ProductoId INT NOT NULL,
    RutaImagen NVARCHAR(255) NOT NULL,
    Orden INT DEFAULT 0,
    EsPrincipal BIT DEFAULT 0,
    CONSTRAINT FK_ImagenesProducto_Producto FOREIGN KEY (ProductoId) 
        REFERENCES Productos(Id) ON DELETE CASCADE,
    INDEX IX_ImagenesProducto_ProductoId (ProductoId)
);

-- ÓRDENES/PEDIDOS
CREATE TABLE Ordenes (
    Id INT PRIMARY KEY IDENTITY(1,1),
    NumeroOrden NVARCHAR(50) NOT NULL UNIQUE,
    ClienteId INT NOT NULL,
    FechaOrden DATETIME2 NOT NULL DEFAULT GETDATE(),
    Estado NVARCHAR(20) NOT NULL DEFAULT 'Pendiente', 
    -- Pendiente, Confirmado, Enviado, Entregado, Cancelado
    Subtotal DECIMAL(18,2) NOT NULL,
    Descuento DECIMAL(18,2) DEFAULT 0,
    Impuesto DECIMAL(18,2) DEFAULT 0,
    CostoEnvio DECIMAL(18,2) DEFAULT 0,
    Total DECIMAL(18,2) NOT NULL,
    DireccionEnvio NVARCHAR(300),
    NotasCliente NVARCHAR(500),
    NotasInternas NVARCHAR(500),
    FechaPago DATETIME2 NULL,
    FechaEnvio DATETIME2 NULL,
    FechaEntrega DATETIME2 NULL,
    NumeroSeguimiento NVARCHAR(100),
    CONSTRAINT FK_Ordenes_Cliente FOREIGN KEY (ClienteId) 
        REFERENCES Clientes(Id),
    INDEX IX_Ordenes_NumeroOrden (NumeroOrden),
    INDEX IX_Ordenes_ClienteId (ClienteId),
    INDEX IX_Ordenes_Estado (Estado),
    INDEX IX_Ordenes_FechaOrden (FechaOrden)
);

-- DETALLE DE ÓRDENES
CREATE TABLE OrdenDetalles (
    Id INT PRIMARY KEY IDENTITY(1,1),
    OrdenId INT NOT NULL,
    ProductoId INT NOT NULL,
    Cantidad INT NOT NULL CHECK (Cantidad > 0),
    PrecioUnitario DECIMAL(18,2) NOT NULL,
    Descuento DECIMAL(18,2) DEFAULT 0,
    Subtotal DECIMAL(18,2) NOT NULL,
    CONSTRAINT FK_OrdenDetalles_Orden FOREIGN KEY (OrdenId) 
        REFERENCES Ordenes(Id) ON DELETE CASCADE,
    CONSTRAINT FK_OrdenDetalles_Producto FOREIGN KEY (ProductoId) 
        REFERENCES Productos(Id),
    INDEX IX_OrdenDetalles_OrdenId (OrdenId),
    INDEX IX_OrdenDetalles_ProductoId (ProductoId)
);

-- PAGOS
CREATE TABLE Pagos (
    Id INT PRIMARY KEY IDENTITY(1,1),
    OrdenId INT NOT NULL,
    MetodoPago NVARCHAR(50) NOT NULL, -- Tarjeta, Transferencia, Efectivo, etc.
    Monto DECIMAL(18,2) NOT NULL,
    FechaPago DATETIME2 NOT NULL DEFAULT GETDATE(),
    Estado NVARCHAR(20) NOT NULL DEFAULT 'Pendiente', -- Pendiente, Aprobado, Rechazado
    TransaccionId NVARCHAR(100),
    DetallesAdicionales NVARCHAR(500),
    CONSTRAINT FK_Pagos_Orden FOREIGN KEY (OrdenId) 
        REFERENCES Ordenes(Id) ON DELETE CASCADE,
    INDEX IX_Pagos_OrdenId (OrdenId),
    INDEX IX_Pagos_Estado (Estado)
);

-- KARDEX / MOVIMIENTOS DE INVENTARIO
CREATE TABLE Kardex (
    Id INT PRIMARY KEY IDENTITY(1,1),
    ProductoId INT NOT NULL,
    TipoMovimiento NVARCHAR(30) NOT NULL, 
    -- ENTRADA_COMPRA, SALIDA_VENTA, AJUSTE_INVENTARIO, DEVOLUCION
    Cantidad INT NOT NULL,
    StockAnterior INT NOT NULL,
    StockNuevo INT NOT NULL,
    Fecha DATETIME2 NOT NULL DEFAULT GETDATE(),
    Referencia NVARCHAR(100), -- ej: "Orden #1234"
    Descripcion NVARCHAR(300),
    UsuarioId NVARCHAR(450), -- quién hizo el movimiento
    CONSTRAINT FK_Kardex_Producto FOREIGN KEY (ProductoId) 
        REFERENCES Productos(Id) ON DELETE CASCADE,
    INDEX IX_Kardex_ProductoId (ProductoId),
    INDEX IX_Kardex_Fecha (Fecha),
    INDEX IX_Kardex_TipoMovimiento (TipoMovimiento)
);

-- ALERTAS DE STOCK
CREATE TABLE AlertasStock (
    Id INT PRIMARY KEY IDENTITY(1,1),
    ProductoId INT NOT NULL,
    Mensaje NVARCHAR(300) NOT NULL,
    Tipo NVARCHAR(20) NOT NULL DEFAULT 'StockBajo', 
    -- StockBajo, StockAgotado, StockCritico
    Fecha DATETIME2 NOT NULL DEFAULT GETDATE(),
    Estado NVARCHAR(20) NOT NULL DEFAULT 'Pendiente', -- Pendiente, Revisado, Resuelto
    FechaRevision DATETIME2 NULL,
    RevisadoPor NVARCHAR(450) NULL,
    CONSTRAINT FK_AlertasStock_Producto FOREIGN KEY (ProductoId) 
        REFERENCES Productos(Id) ON DELETE CASCADE,
    INDEX IX_AlertasStock_Estado (Estado),
    INDEX IX_AlertasStock_Fecha (Fecha)
);

-- CARRITOS DE COMPRA (para sesiones)
CREATE TABLE Carritos (
    Id INT PRIMARY KEY IDENTITY(1,1),
    UsuarioId NVARCHAR(450) NULL, -- si está logueado
    SessionId NVARCHAR(100) NULL, -- si es anónimo
    FechaCreacion DATETIME2 NOT NULL DEFAULT GETDATE(),
    FechaModificacion DATETIME2 NOT NULL DEFAULT GETDATE(),
    INDEX IX_Carritos_UsuarioId (UsuarioId),
    INDEX IX_Carritos_SessionId (SessionId)
);

-- ITEMS DEL CARRITO
CREATE TABLE CarritoItems (
    Id INT PRIMARY KEY IDENTITY(1,1),
    CarritoId INT NOT NULL,
    ProductoId INT NOT NULL,
    Cantidad INT NOT NULL CHECK (Cantidad > 0),
    FechaAgregado DATETIME2 NOT NULL DEFAULT GETDATE(),
    CONSTRAINT FK_CarritoItems_Carrito FOREIGN KEY (CarritoId) 
        REFERENCES Carritos(Id) ON DELETE CASCADE,
    CONSTRAINT FK_CarritoItems_Producto FOREIGN KEY (ProductoId) 
        REFERENCES Productos(Id),
    INDEX IX_CarritoItems_CarritoId (CarritoId)
);

GO

-- ==============================================
-- VISTAS ÚTILES
-- ==============================================

-- Vista de productos con stock bajo
CREATE VIEW vw_ProductosStockBajo AS
SELECT 
    p.Id,
    p.SKU,
    p.Nombre,
    p.Stock,
    p.StockMinimo,
    c.Nombre AS Categoria,
    m.Nombre AS Marca
FROM Productos p
LEFT JOIN Categorias c ON p.CategoriaId = c.Id
LEFT JOIN Marcas m ON p.MarcaId = m.Id
WHERE p.Stock <= p.StockMinimo AND p.Activo = 1;

GO

-- Vista de órdenes con información completa
CREATE VIEW vw_OrdenesCompletas AS
SELECT 
    o.Id,
    o.NumeroOrden,
    o.FechaOrden,
    o.Estado,
    o.Total,
    c.NombreCompleto AS Cliente,
    c.Email AS EmailCliente,
    COUNT(od.Id) AS TotalProductos,
    SUM(od.Cantidad) AS TotalUnidades
FROM Ordenes o
INNER JOIN Clientes c ON o.ClienteId = c.Id
LEFT JOIN OrdenDetalles od ON o.Id = od.OrdenId
GROUP BY o.Id, o.NumeroOrden, o.FechaOrden, o.Estado, o.Total, c.NombreCompleto, c.Email;

GO

-- ==============================================
-- PROCEDIMIENTOS ALMACENADOS
-- ==============================================

-- SP para obtener productos más vendidos
CREATE PROCEDURE sp_ProductosMasVendidos
    @Top INT = 10,
    @FechaInicio DATETIME2 = NULL,
    @FechaFin DATETIME2 = NULL
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT TOP (@Top)
        p.Id,
        p.SKU,
        p.Nombre,
        SUM(od.Cantidad) AS TotalVendido,
        SUM(od.Subtotal) AS VentaTotal,
        COUNT(DISTINCT od.OrdenId) AS NumeroOrdenes
    FROM Productos p
    INNER JOIN OrdenDetalles od ON p.Id = od.ProductoId
    INNER JOIN Ordenes o ON od.OrdenId = o.Id
    WHERE o.Estado IN ('Entregado', 'Enviado')
        AND (@FechaInicio IS NULL OR o.FechaOrden >= @FechaInicio)
        AND (@FechaFin IS NULL OR o.FechaOrden <= @FechaFin)
    GROUP BY p.Id, p.SKU, p.Nombre
    ORDER BY TotalVendido DESC;
END;

GO

-- SP para reporte de ventas por período
CREATE PROCEDURE sp_ReporteVentas
    @FechaInicio DATETIME2,
    @FechaFin DATETIME2
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT 
        CAST(o.FechaOrden AS DATE) AS Fecha,
        COUNT(o.Id) AS TotalOrdenes,
        SUM(o.Total) AS VentaTotal,
        AVG(o.Total) AS PromedioOrden,
        SUM(od.Cantidad) AS UnidadesVendidas
    FROM Ordenes o
    LEFT JOIN OrdenDetalles od ON o.Id = od.OrdenId
    WHERE o.FechaOrden BETWEEN @FechaInicio AND @FechaFin
        AND o.Estado IN ('Entregado', 'Enviado', 'Confirmado')
    GROUP BY CAST(o.FechaOrden AS DATE)
    ORDER BY Fecha DESC;
END;

GO

-- ==============================================
-- DATOS INICIALES (SEED DATA)
-- ==============================================

-- Categorías principales
INSERT INTO Categorias (Nombre, Descripcion, ParentId, Orden) VALUES
('Electrónica', 'Dispositivos electrónicos y accesorios', NULL, 1),
('Ropa', 'Ropa y accesorios de moda', NULL, 2),
('Hogar', 'Artículos para el hogar', NULL, 3),
('Deportes', 'Equipamiento deportivo', NULL, 4),
('Libros', 'Libros y material educativo', NULL, 5);

-- Subcategorías
INSERT INTO Categorias (Nombre, Descripcion, ParentId, Orden) VALUES
('Smartphones', 'Teléfonos inteligentes', 1, 1),
('Laptops', 'Computadoras portátiles', 1, 2),
('Accesorios Tech', 'Cables, cargadores, fundas', 1, 3),
('Ropa Hombre', 'Ropa para caballero', 2, 1),
('Ropa Mujer', 'Ropa para dama', 2, 2),
('Calzado', 'Zapatos y zapatillas', 2, 3);

-- Marcas
INSERT INTO Marcas (Nombre, Descripcion) VALUES
('Apple', 'Innovación tecnológica'),
('Samsung', 'Electrónica de calidad'),
('Nike', 'Just Do It'),
('Adidas', 'Impossible is Nothing'),
('Sony', 'Entretenimiento y tecnología');

-- Proveedores
INSERT INTO Proveedores (Nombre, Contacto, Email, Telefono) VALUES
('Tech Imports SA', 'Juan Pérez', 'ventas@techimports.com', '2222-3333'),
('Fashion Wholesale', 'María López', 'info@fashionwh.com', '2222-4444'),
('Home Supplies Co', 'Carlos Rodríguez', 'carlos@homesupply.com', '2222-5555');

GO

PRINT 'Base de datos creada exitosamente';