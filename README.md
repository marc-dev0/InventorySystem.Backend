# Inventory System

Sistema de gestión de inventario desarrollado en .NET 9 con arquitectura limpia (Clean Architecture).

## Descripción

Sistema completo de gestión de inventario que permite administrar productos, stock, ventas, compras, clientes y proveedores. Incluye funcionalidades de importación de datos, seguimiento de movimientos de inventario y autenticación de usuarios.

## Arquitectura

El proyecto utiliza Clean Architecture con las siguientes capas:

- **InventorySystem.API**: Capa de presentación (Web API)
- **InventorySystem.Application**: Lógica de aplicación y servicios
- **InventorySystem.Core**: Entidades del dominio e interfaces
- **InventorySystem.Infrastructure**: Acceso a datos y servicios externos

## Tecnologías

- **.NET 9.0**
- **Entity Framework Core 9.0**
- **PostgreSQL**
- **JWT Authentication**
- **Serilog** para logging
- **Swagger/OpenAPI** para documentación
- **Docker** para contenedorización

## Características

### Gestión de Entidades
- **Productos**: CRUD completo con categorías
- **Stock**: Control de inventario por tienda y almacén
- **Ventas**: Registro de ventas con detalles
- **Compras**: Gestión de compras a proveedores
- **Clientes y Proveedores**: Administración de contactos
- **Usuarios**: Sistema de autenticación y autorización

### Funcionalidades Avanzadas
- Importación masiva de datos desde Excel/CSV
- Seguimiento de movimientos de inventario
- Reportes de stock y ventas
- API RESTful completa
- Logging estructurado
- Health checks

## Requisitos Previos

- .NET 9.0 SDK
- PostgreSQL 12+
- Docker (opcional)

## Configuración

### Base de Datos

1. Instalar PostgreSQL
2. Crear base de datos `inventory_db`
3. Configurar cadena de conexión en `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=inventory_db;Username=usuario;Password=contraseña"
  }
}
```

### Variables de Entorno

```bash
ASPNETCORE_ENVIRONMENT=Development
ConnectionStrings__DefaultConnection="Host=localhost;Port=5432;Database=inventory_db;Username=usuario;Password=contraseña"
```

## Instalación y Ejecución

### Desarrollo Local

```bash
# Clonar repositorio
git clone <repository-url>
cd InventorySystem

# Restaurar dependencias
dotnet restore

# Aplicar migraciones
dotnet ef database update --project InventorySystem.Infrastructure --startup-project InventorySystem.API

# Ejecutar aplicación
dotnet run --project InventorySystem.API
```

### Docker

```bash
# Construir imagen
docker build -t inventory-system .

# Ejecutar contenedor
docker run -p 8080:8080 inventory-system
```

### Scripts de Utilidad

- `setup-inventory.sh`: Configuración inicial del proyecto
- `seed-data.sh`: Cargar datos de prueba
- `clear-all-data.sql`: Limpiar todas las tablas
- `clear-only-stock.sql`: Limpiar solo datos de stock

## API Endpoints

### Autenticación
- `POST /api/auth/login` - Iniciar sesión
- `POST /api/auth/register` - Registrar usuario

### Productos
- `GET /api/products` - Listar productos
- `POST /api/products` - Crear producto
- `PUT /api/products/{id}` - Actualizar producto
- `DELETE /api/products/{id}` - Eliminar producto

### Stock
- `GET /api/stock` - Consultar stock
- `POST /api/stock/initial` - Registro inicial de stock

### Ventas
- `GET /api/sales` - Listar ventas
- `POST /api/sales` - Registrar venta

### Compras
- `GET /api/purchases` - Listar compras
- `POST /api/purchases` - Registrar compra

### Importación
- `POST /api/tandia-import` - Importar productos desde Tandia
- `GET /api/tandia-import/tracking` - Seguimiento de importaciones

## Documentación API

Una vez ejecutada la aplicación, la documentación Swagger estará disponible en:
- `https://localhost:5001/swagger` (HTTPS)
- `http://localhost:5000/swagger` (HTTP)

## Estructura del Proyecto

```
InventorySystem/
├── InventorySystem.API/           # Web API
│   ├── Controllers/               # Controladores
│   ├── Configuration/             # Configuración JWT
│   └── Utilities/                # Utilidades
├── InventorySystem.Application/   # Lógica de aplicación
│   ├── DTOs/                     # Data Transfer Objects
│   ├── Interfaces/               # Interfaces de servicios
│   └── Services/                 # Implementación de servicios
├── InventorySystem.Core/         # Dominio
│   ├── Entities/                 # Entidades del dominio
│   ├── Interfaces/               # Interfaces de repositorios
│   └── Enums/                    # Enumeraciones
├── InventorySystem.Infrastructure/ # Acceso a datos
│   ├── Data/                     # DbContext
│   ├── Repositories/             # Implementación de repositorios
│   ├── Services/                 # Servicios de infraestructura
│   └── Migrations/               # Migraciones EF
└── cargas/                       # Archivos de importación
```

## Base de Datos

### Entidades Principales

- **Product**: Productos del inventario
- **ProductStock**: Stock por producto y tienda
- **Sale/SaleDetail**: Ventas y sus detalles
- **Purchase/PurchaseDetail**: Compras y sus detalles
- **Customer**: Clientes
- **Supplier**: Proveedores
- **Category**: Categorías de productos
- **User**: Usuarios del sistema
- **InventoryMovement**: Movimientos de inventario

## Logging

El sistema utiliza Serilog para logging estructurado:
- Console output para desarrollo
- Archivos rotativos en `logs/`
- Diferentes niveles de logging configurables

## Despliegue

### Railway

El proyecto incluye configuración para despliegue en Railway:
- `railway.json`: Configuración del servicio
- Variables de entorno configuradas
- Base de datos PostgreSQL incluida

### Variables de Entorno de Producción

```
ASPNETCORE_ENVIRONMENT=Production
ConnectionStrings__DefaultConnection=<postgres-connection-string>
JwtSettings__SecretKey=<secure-secret-key>
```

## Desarrollo

### Convenciones de Código
- Seguir convenciones de C# y .NET
- Usar async/await para operaciones asíncronas
- Implementar manejo de errores con try-catch
- Validar datos de entrada en DTOs

### Testing
```bash
# Ejecutar tests (cuando estén implementados)
dotnet test
```

### Migraciones
```bash
# Crear nueva migración
dotnet ef migrations add NombreMigracion --project InventorySystem.Infrastructure --startup-project InventorySystem.API

# Aplicar migraciones
dotnet ef database update --project InventorySystem.Infrastructure --startup-project InventorySystem.API
```

## Contribución

1. Fork del proyecto
2. Crear rama de feature (`git checkout -b feature/nueva-funcionalidad`)
3. Commit de cambios (`git commit -am 'Agregar nueva funcionalidad'`)
4. Push a la rama (`git push origin feature/nueva-funcionalidad`)
5. Crear Pull Request

## Licencia

Este proyecto es de uso interno.

## Contacto

Para soporte o consultas, contactar al equipo de desarrollo.