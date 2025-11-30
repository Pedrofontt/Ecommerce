📦 EcommerceSystem — Guía de Ejecución y Estructura del Proyecto

Este documento explica cómo ejecutar el proyecto EcommerceSystem, cómo está estructurado y cómo utilizar las migraciones y el archivo .sql incluido para generar la base de datos desde cero.

📁 Estructura del Proyecto

El proyecto sigue el patrón estándar de ASP.NET Core MVC con Identity.
Los elementos más importantes son:

EcommerceSystem/
│
├── Controllers/
├── Models/
├── Views/
├── Data/
│   └── ApplicationDbContext.cs
│
├── Migrations/   ← IMPORTANTE
│   ├── 20251123033706_InitialCreate.cs
│   ├── 20251123033706_InitialCreate.Designer.cs
│   └── ApplicationDbContextModelSnapshot.cs
│
├── Database/
│   └── TextFile.sql   ← IMPORTANTE
│
├── wwwroot/
├── appsettings.json
├── Program.cs
└── EcommerceSystem.sln

🔑 ASP.NET Core Identity

El sistema utiliza ASP.NET Core Identity, lo cual implica:

Tablas predefinidas como:

AspNetUsers

AspNetRoles

AspNetUserRoles

AspNetUserClaims

AspNetRoleClaims

AspNetUserTokens

El contexto ApplicationDbContext hereda de IdentityDbContext, lo que habilita automáticamente el esquema de autenticación.

En la carpeta Migrations encontrarás la migración llamada:

20251123033706_InitialCreate.cs


La cual contiene toda la estructura de la base de datos, incluyendo:

Tablas Identity

Tablas personalizadas del sistema (si aplican)

Relaciones

Llaves primarias y foráneas

🛢 Base de Datos — Archivo SQL (Importante)

El proyecto incluye un archivo:

Database/TextFile.sql


Este archivo tiene como objetivo permitirte crear la base de datos completa desde un script, 

✔ Debes asegurarte de que este archivo contenga todas las instrucciones CREATE TABLE, llaves, restricciones y datos iniciales
✔ Si haces cambios en el modelo, recuerda actualizar este .sql o regenerarlo desde las migraciones

Cómo generar este archivo desde EF Migrations

Si deseas regenerarlo, puedes usar este comando:

dotnet ef migrations script -o Database/TextFile.sql

Tambien se puede usar: 

Add-Migration InitialCreate
Update-Database

Esto creará un script SQL con:

Estructura completa de tablas

Relaciones

Índices

Creates / Alters necesarios

▶️ Cómo Ejecutar el Proyecto
1. Instalar requisitos

Necesitas:

 .NET 9 SDK 

SQL Server (LocalDB, Express o full)

Visual Studio 

2. Configurar la cadena de conexión

En appsettings.json:

"ConnectionStrings": {
  "DefaultConnection": "Server=localhost;Database=EcommerceSystem;Trusted_Connection=True;MultipleActiveResultSets=true"
}


Modifica según tu instalación.

3. Crear la base de datos

Tienes dos opciones:

✅ Opción A: Usar las migraciones (recomendado)

En la carpeta raíz del proyecto:




Esto:

Creará la base de datos

Ejecutará la migración InitialCreate

Creará todas las tablas de Identity y del sistema

✅ Opción B: Usar el archivo SQL incluido

Abre SQL Server Management Studio (SSMS)

Crea una base de datos vacía manualmente (sin tablas)

Abre el archivo:

Database/TextFile.sql

## Es importante que AspNetUsers tenga el usuario admin creado para poder iniciar sesión ya que desde la pagina solo se pueden crear usuarios normales.

Ejecuta el script completo

Esto reproducirá la estructura generada por Identity y tus modelos.

▶️ 4. Ejecutar el sistema

Desde la consola:

dotnet run


O desde Visual Studio:

👉 Presiona F5 o clic en Start.

El sistema abrirá en:

https://localhost:xxxx

✔️ Conclusión

Este proyecto utiliza:

ASP.NET Core MVC

ASP.NET Core Identity

EF Core con migraciones

Script SQL para recrear la base de datos

La carpeta Migrations más el archivo TextFile.sql son elementos clave para administrar y desplegar la base de datos en cualquier entorno.