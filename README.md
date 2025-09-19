# 🔧 InventorySystem.Backend

> API REST desarrollada en .NET 9 con Clean Architecture para gestión empresarial de inventarios

![.NET 9](https://img.shields.io/badge/.NET-9.0-purple)
![EF Core](https://img.shields.io/badge/EF%20Core-9.0-orange)
![PostgreSQL](https://img.shields.io/badge/PostgreSQL-12+-green)
![Clean Architecture](https://img.shields.io/badge/Architecture-Clean-blue)

## 📋 Tabla de Contenidos

- [🎯 Descripción](#-descripción)
- [🏗️ Arquitectura](#️-arquitectura)
- [🚀 Inicio Rápido](#-inicio-rápido)
- [📁 Estructura del Proyecto](#-estructura-del-proyecto)
- [🔧 Tecnologías](#-tecnologías)
- [📊 Base de Datos](#-base-de-datos)
- [🛡️ Autenticación](#️-autenticación)
- [📡 API Endpoints](#-api-endpoints)
- [📦 Importación de Datos](#-importación-de-datos)
- [🔍 Logging y Monitoreo](#-logging-y-monitoreo)
- [🧪 Testing](#-testing)
- [🚀 Deployment](#-deployment)

## 🎯 Descripción

API backend para sistema de inventario empresarial que maneja:

- **Gestión de productos** con stock multi-tienda
- **Importación masiva** desde archivos Excel/CSV (formato Tandia)
- **Tracking completo** de movimientos de inventario
- **Autenticación JWT** con roles de usuario
- **Arquitectura escalable** preparada para crecimiento

### 🎪 Características Especiales

- **Multi-tienda inteligente**: Auto-creación de stock en todas las tiendas
- **Importación robusta**: Procesamiento por lotes con validación
- **Consistencia de datos**: Eliminación de duplicados automática
- **Auditoría completa**: Logs estructurados de todas las operaciones

## 🏗️ Arquitectura

### Clean Architecture en 4 Capas

```
┌─────────────────────────────────────────────────────────────┐
│                    InventorySystem.API                      │
│  Controllers, Middleware, Configuration, Startup           │
│  ├── ProductsController     ├── AuthController            │
│  ├── TandiaImportController ├── StockController           │
│  └── BackgroundJobsController                             │
└─────────────────────────────────────────────────────────────┘
                              │
┌─────────────────────────────────────────────────────────────┐
│                InventorySystem.Application                  │
│  Services, DTOs, Interfaces, Business Logic               │
│  ├── TandiaImportService    ├── ProductService            │
│  ├── StockInitialService    ├── SaleService               │
│  └── AuthService                                          │
└─────────────────────────────────────────────────────────────┘
                              │
┌─────────────────────────────────────────────────────────────┐
│                  InventorySystem.Core                       │
│  Entities, Enums, Domain Interfaces                       │
│  ├── Product               ├── ProductStock               │
│  ├── Sale                  ├── InventoryMovement          │
│  └── User                                                 │
└─────────────────────────────────────────────────────────────┘
                              │
┌─────────────────────────────────────────────────────────────┐
│              InventorySystem.Infrastructure                 │
│  Repositories, DbContext, External Services               │
│  ├── InventoryDbContext     ├── ProductRepository         │
│  ├── Migrations             ├── StockRepository           │
│  └── ExternalApiService                                   │
└─────────────────────────────────────────────────────────────┘
```

### 🔄 Flujo de Datos

```
HTTP Request → Controller → Service → Repository → Database
     ↑                                                ↓
JSON Response ← DTO ← Business Logic ← Entity ← EF Core
```

## 🚀 Inicio Rápido

### 📋 Prerequisites

```bash
# Verificar versiones
dotnet --version  # >= 9.0
psql --version   # >= 12.0
```

### 1️⃣ Configuración de la Base de Datos

```bash
# Crear base de datos PostgreSQL
createdb InventorySystemDB

# Configurar connection string en appsettings.json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=InventorySystemDB;Username=tu_usuario;Password=tu_password"
  }
}
```

### 2️⃣ Restaurar y Ejecutar

```bash
# Restaurar dependencias
dotnet restore

# Aplicar migraciones
dotnet ef database update

# Ejecutar en desarrollo
dotnet run --project InventorySystem.API

# O con hot reload
dotnet watch --project InventorySystem.API
```

### 3️⃣ Verificar Instalación

```bash
# API Health Check
curl http://localhost:5000/health

# Swagger UI
open http://localhost:5000/swagger
```

## 📁 Estructura del Proyecto

```
InventorySystem.Backend/
├── 📂 InventorySystem.API/              # 🌐 Web API Layer
│   ├── 📂 Controllers/                  #   REST Controllers
│   │   ├── ProductsController.cs        #   CRUD de productos
│   │   ├── TandiaImportController.cs    #   Importación de datos
│   │   ├── BackgroundJobsController.cs  #   Jobs asíncronos
│   │   └── AuthController.cs            #   Autenticación JWT
│   ├── 📂 Middleware/                   #   Custom middleware
│   ├── 📂 Utilities/                    #   Helpers y validadores
│   ├── 📄 Program.cs                    #   Configuración de la app
│   └── 📄 appsettings.json             #   Configuración
├── 📂 InventorySystem.Application/      # 💼 Business Logic Layer
│   ├── 📂 Services/                     #   Servicios de negocio
│   │   ├── TandiaImportService.cs       #   Importación Excel
│   │   ├── ProductService.cs            #   Lógica de productos
│   │   ├── StockInitialService.cs       #   Gestión de stock
│   │   └── AuthService.cs               #   Autenticación
│   ├── 📂 DTOs/                         #   Data Transfer Objects
│   ├── 📂 Interfaces/                   #   Contratos de servicios
│   └── 📂 Mappings/                     #   AutoMapper profiles
├── 📂 InventorySystem.Core/             # 🏛️ Domain Layer
│   ├── 📂 Entities/                     #   Entidades del dominio
│   │   ├── Product.cs                   #   Producto
│   │   ├── ProductStock.cs              #   Stock por tienda
│   │   ├── Sale.cs                      #   Venta
│   │   ├── InventoryMovement.cs         #   Movimiento de inventario
│   │   └── User.cs                      #   Usuario
│   ├── 📂 Enums/                        #   Enumeraciones
│   └── 📂 Interfaces/                   #   Contratos del dominio
├── 📂 InventorySystem.Infrastructure/   # 🗄️ Data Access Layer
│   ├── 📂 Data/                         #   Contexto de EF
│   │   └── InventoryDbContext.cs        #   DbContext principal
│   ├── 📂 Repositories/                 #   Implementación de repos
│   │   ├── ProductRepository.cs         #   Repositorio de productos
│   │   └── StockRepository.cs           #   Repositorio de stock
│   ├── 📂 Migrations/                   #   Migraciones de EF
│   └── 📂 Configurations/               #   Configuraciones de entidades
├── 📄 InventorySystem.sln              # Solution file
├── 📄 Dockerfile                       # Contenedor Docker
├── 📄 README.md                        # Esta documentación
└── 📄 CAMBIOS_STOCK_MANAGEMENT.md      # Refactorización reciente
```

## 🔧 Tecnologías

### Core Framework
- **.NET 9.0** - Framework principal
- **ASP.NET Core** - Web API framework
- **Entity Framework Core 9.0** - ORM
- **PostgreSQL** - Base de datos relacional

### Librerías Principales
```xml
<PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="9.0.0" />
<PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="9.0.0" />
<PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="9.0.0" />
<PackageReference Include="ClosedXML" Version="0.102.2" />
<PackageReference Include="Serilog.AspNetCore" Version="8.0.2" />
<PackageReference Include="Swashbuckle.AspNetCore" Version="6.8.1" />
```

### Herramientas de Desarrollo
- **Swagger/OpenAPI** - Documentación automática de API
- **Serilog** - Logging estructurado
- **Health Checks** - Monitoreo de servicios
- **CORS** - Cross-Origin Resource Sharing

## 📊 Base de Datos

### 🗂️ Entidades Principales

```sql
-- Productos únicos sin stock local
CREATE TABLE Products (
    Id SERIAL PRIMARY KEY,
    Code VARCHAR(50) UNIQUE NOT NULL,
    Name VARCHAR(255) NOT NULL,
    Description TEXT,
    PurchasePrice DECIMAL(18,2),
    SalePrice DECIMAL(18,2),
    Unit VARCHAR(20),
    CategoryId INT REFERENCES Categories(Id),
    Active BOOLEAN DEFAULT true
);

-- Stock distribuido por tienda
CREATE TABLE ProductStocks (
    Id SERIAL PRIMARY KEY,
    ProductId INT REFERENCES Products(Id),
    StoreId INT REFERENCES Stores(Id),
    CurrentStock DECIMAL(18,3),
    MinimumStock DECIMAL(18,3),
    MaximumStock DECIMAL(18,3),
    AverageCost DECIMAL(18,2),
    UNIQUE(ProductId, StoreId)
);

-- Tracking completo de movimientos
CREATE TABLE InventoryMovements (
    Id SERIAL PRIMARY KEY,
    ProductId INT REFERENCES Products(Id),
    StoreId INT REFERENCES Stores(Id),
    ProductStockId INT REFERENCES ProductStocks(Id),
    Type VARCHAR(20) NOT NULL, -- Sale, Purchase, Transfer, Adjustment
    Quantity DECIMAL(18,3),
    PreviousStock DECIMAL(18,3),
    NewStock DECIMAL(18,3),
    Date TIMESTAMP,
    Reason TEXT,
    DocumentNumber VARCHAR(100)
);
```

### 📈 Diseño Multi-Tienda

```
Product (Code: ABC123)
├── ProductStock (Store: MAIN, Stock: 100)
├── ProductStock (Store: TANT, Stock: 50)
└── ProductStock (Store: WH01, Stock: 200)

Total Stock = 350 units
```

### 🔄 Migraciones

```bash
# Crear nueva migración
dotnet ef migrations add NombreMigracion

# Aplicar migraciones
dotnet ef database update

# Rollback a migración específica
dotnet ef database update MigracionAnterior

# Generar script SQL
dotnet ef migrations script
```

## 🛡️ Autenticación

### JWT Configuration

```json
{
  "Jwt": {
    "Key": "tu-clave-secreta-super-segura-de-al-menos-32-caracteres",
    "Issuer": "InventorySystem",
    "Audience": "InventorySystemUsers",
    "ExpiryInMinutes": 60
  }
}
```

### Roles de Usuario

```csharp
public enum UserRole
{
    Admin,    // Acceso completo + importaciones
    User      // Operaciones básicas (sin eliminar)
}
```

### Endpoints de Auth

```bash
# Login
POST /api/auth/login
{
  "username": "admin",
  "password": "password"
}

# Respuesta
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "expiration": "2025-09-18T10:30:00Z",
  "user": { "id": 1, "username": "admin", "role": "Admin" }
}
```

## 📡 API Endpoints

### 📦 Productos

```bash
# Listar productos con paginación
GET /api/products?page=1&pageSize=20&search=codigo&categoryId=1

# Obtener producto por ID
GET /api/products/{id}

# Crear producto
POST /api/products
{
  "code": "ABC123",
  "name": "Producto Ejemplo",
  "purchasePrice": 10.00,
  "salePrice": 15.00,
  "categoryId": 1
}

# Actualizar producto
PUT /api/products/{id}

# Eliminar producto
DELETE /api/products/{id}
```

### 🏪 Stock Multi-Tienda

```bash
# Stock por producto y tienda
GET /api/productstocks/product/{productId}/store/{storeId}

# Actualizar stock específico
PUT /api/productstocks/{id}
{
  "currentStock": 100,
  "minimumStock": 10
}

# Movimientos de inventario
GET /api/inventorymovements?productId={id}&storeId={id}
```

### 📊 Importación de Datos

```bash
# Importar productos (Admin only)
POST /api/tandiaimport/products
Content-Type: multipart/form-data
file: productos_tienda.xlsx

# Importar stock inicial
POST /api/stockinitial/load/{storeCode}
Content-Type: multipart/form-data
file: stock_tienda.xlsx

# Importar ventas
POST /api/tandiaimport/sales?storeCode=MAIN
Content-Type: multipart/form-data
file: ventas_tienda.xlsx
```

### 🔍 Reportes y Analytics

```bash
# Estadísticas de productos
GET /api/products/stats

# Productos con stock bajo
GET /api/products/low-stock

# Historial de importaciones
GET /api/tandiaimport/import-batches?page=1&type=PRODUCTS
```

## 📦 Importación de Datos

### 🎯 Flujo de Importación Inteligente

#### 1️⃣ Importación de Productos
```
Excel: productos_tienda.xlsx
├── Código duplicado → Se crea SOLO UNA VEZ
├── Múltiples precios → Se usa el primer precio encontrado
└── Auto-creación → ProductStock en TODAS las tiendas (stock=0)
```

#### 2️⃣ Importación de Stock
```
Excel: stock_tienda.xlsx
├── Por tienda específica (storeCode)
├── Solo actualiza ProductStock existente
└── No afecta otras tiendas
```

#### 3️⃣ Importación de Ventas
```
Excel: ventas_tienda.xlsx
├── Agrupa por número de documento
├── Actualiza stock de tienda específica
└── Crea movimientos de inventario
```

### 📋 Formato de Archivos Excel

#### Productos (columnas esperadas):
```
A: Tienda | B: Código | C: Cod.barras | D: Nombre | E: Descripción
F: Categorias | G: Marca | H: Características | I: Impuestos | J: P.costo
K: Estado | L: Stock | M: Stock min | N: Ubicación | O: P.venta
```

#### Stock Inicial:
```
A: Código | B: Stock | C: Ubicación
```

#### Ventas:
```
A: Documento | B: Fecha | C: Cliente | D: Producto | E: Cantidad
F: Precio | G: Total | H: Impuesto
```

### 🔄 Batch Processing

```csharp
// Cada importación genera un ImportBatch
public class ImportBatch
{
    public string BatchCode { get; set; }     // PRODUCTS_20250917_001
    public string BatchType { get; set; }    // PRODUCTS, SALES, STOCK_INITIAL
    public string FileName { get; set; }
    public int TotalRecords { get; set; }
    public int SuccessCount { get; set; }
    public int ErrorCount { get; set; }
    public DateTime ImportDate { get; set; }
    public string ImportedBy { get; set; }
}
```

## 🔍 Logging y Monitoreo

### Serilog Configuration

```json
{
  "Serilog": {
    "Using": ["Serilog.Sinks.Console", "Serilog.Sinks.File"],
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "System": "Warning"
      }
    },
    "WriteTo": [
      { "Name": "Console" },
      {
        "Name": "File",
        "Args": {
          "path": "logs/log-.txt",
          "rollingInterval": "Day"
        }
      }
    ]
  }
}
```

### Health Checks

```bash
# Verificar salud de la API
GET /health

# Respuesta
{
  "status": "Healthy",
  "checks": {
    "database": "Healthy",
    "memory": "Healthy"
  },
  "duration": "00:00:00.0123456"
}
```

### Logs Estructurados

```csharp
_logger.LogInformation("Product {ProductCode} imported for store {StoreCode} in batch {BatchCode}",
    product.Code, storeCode, batchCode);
```

## 🧪 Testing

### Unit Tests Structure

```
InventorySystem.Tests/
├── 📂 Unit/
│   ├── Services/
│   │   ├── ProductServiceTests.cs
│   │   └── TandiaImportServiceTests.cs
│   └── Repositories/
│       └── ProductRepositoryTests.cs
├── 📂 Integration/
│   ├── Controllers/
│   │   └── ProductsControllerTests.cs
│   └── Database/
│       └── DatabaseTests.cs
└── 📂 TestFixtures/
    ├── TestData.cs
    └── DatabaseFixture.cs
```

### Ejecutar Tests

```bash
# Todos los tests
dotnet test

# Tests específicos
dotnet test --filter "TestCategory=Unit"

# Con cobertura
dotnet test --collect:"XPlat Code Coverage"
```

## 🚀 Deployment

### 🐳 Docker

```dockerfile
# Dockerfile incluido
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore
RUN dotnet build -c Release

FROM build AS publish
RUN dotnet publish -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "InventorySystem.API.dll"]
```

```bash
# Build y run
docker build -t inventory-backend .
docker run -p 8080:8080 inventory-backend
```

### 🌊 Producción

```bash
# Configurar para producción
export ASPNETCORE_ENVIRONMENT=Production

# Compilar para release
dotnet publish -c Release -o ./publish

# Ejecutar
cd publish
dotnet InventorySystem.API.dll
```

### Variables de Entorno

```bash
# Database
DATABASE_CONNECTION="Host=prod-db;Database=InventorySystem;Username=api_user;Password=secure_password"

# JWT
JWT_KEY="production-super-secure-key-with-at-least-32-characters"
JWT_ISSUER="InventorySystem.Production"

# Logging
SERILOG_MINIMUMLEVEL="Warning"
```

## 📚 Referencias Adicionales

- [📄 Cambios de Stock Management](./CAMBIOS_STOCK_MANAGEMENT.md) - Refactorización reciente
- [🏗️ Entity Framework Migrations](./InventorySystem.Infrastructure/Migrations/)
- [📊 Swagger API Docs](http://localhost:5000/swagger) - Cuando está corriendo
- [🔧 Configuración Docker](./Dockerfile)

## 🐛 Troubleshooting

### Problemas Comunes

```bash
# Error de migración
dotnet ef database drop  # ⚠️ Cuidado en producción
dotnet ef database update

# Error de dependencias
dotnet clean
dotnet restore

# Puerto ocupado
netstat -tulpn | grep :5000
kill -9 <PID>
```

### Logs para Debug

```bash
# Ver logs en tiempo real
tail -f logs/log-20250917.txt

# Logs de Entity Framework
export Logging__LogLevel__Microsoft.EntityFrameworkCore=Information
```

---

**InventorySystem.Backend** - API robusta para gestión empresarial de inventarios 🚀