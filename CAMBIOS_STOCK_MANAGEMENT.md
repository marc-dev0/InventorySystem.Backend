# Refactorización del Manejo de Stock

## Problema Identificado

### Situación Anterior:
1. **Campos duplicados**: Se manejaba stock tanto en `Products.Stock` como en `ProductStocks.CurrentStock`
2. **Productos duplicados en Excel**: Los archivos de carga contienen el mismo código de producto con diferentes listas de precios
3. **Inconsistencia**: `ProductStocks` solo se creaban bajo demanda durante ventas o movimientos específicos
4. **Stock por tienda incompleto**: Al cargar productos para una tienda, no se creaban registros en `ProductStocks` para todas las tiendas

### Archivos de Carga Analizados:
- `carga_productos_almacen.xlsx`
- `carga_productos_tienda.xlsx`
- Mismo código aparece múltiples veces con diferentes precios y unidades

## Solución Implementada

### 1. Eliminación de Campos Deprecados
- **Removido**: `Products.Stock` (obsoleto)
- **Removido**: `Products.MinimumStock` (obsoleto)
- **Mantenido**: Solo `ProductStocks` maneja el stock por tienda

### 2. Nueva Lógica de Importación
- **Productos únicos**: Se crea un solo producto por código (no duplicados)
- **ProductStocks automáticos**: Al crear un producto, se generan automáticamente registros en `ProductStocks` para **todas las tiendas existentes**
- **Stock inicial cero**: Todos los productos empiezan con stock = 0
- **Stock se maneja separadamente**: Usar carga de stock inicial para establecer inventarios

### 3. Archivos Modificados

#### Entidades:
- `InventorySystem.Core/Entities/Product.cs` - Eliminados campos Stock y MinimumStock

#### Migraciones:
- `20250917232600_RemoveDeprecatedStockFields.cs` - Nueva migración para eliminar columnas

#### Servicios:
- `TandiaImportService.cs` - Agregado método `CreateProductStocksForAllStoresAsync()`
- `StockInitialService.cs` - Eliminada lógica de actualización de Product.Stock
- `ProductRepository.cs` - Removidas referencias a Product.Stock

#### Controllers:
- `FastImportsController.cs` - Eliminadas asignaciones a Product.Stock
- `TandiaImportController.cs` - Comentadas estadísticas basadas en Product.Stock

#### DTOs:
- `ProductDto.cs` - Eliminado campo Stock deprecated, mantenido CurrentStock

## Flujo Actualizado

### Importación de Productos:
1. Leer archivo Excel de productos
2. Por cada código de producto único:
   - Crear registro en `Products` (sin duplicados)
   - Crear registros en `ProductStocks` para **todas las tiendas** con stock = 0
3. Skipear productos duplicados en el Excel

### Importación de Stock:
1. Leer archivo Excel de stock inicial
2. Actualizar `ProductStocks.CurrentStock` por tienda específica
3. No tocar tabla `Products`

### Consultas de Stock:
- **Stock total**: Sumar `ProductStocks.CurrentStock` de todas las tiendas
- **Stock por tienda**: Consultar directamente `ProductStocks`
- **Stock mínimo**: Usar `ProductStocks.MinimumStock`

## Ventajas de la Nueva Arquitectura

1. **Consistencia**: Un solo lugar para manejar stock (ProductStocks)
2. **Completitud**: Todos los productos tienen stock en todas las tiendas
3. **Escalabilidad**: Fácil agregar nuevas tiendas
4. **Claridad**: Separación clara entre productos y su stock por ubicación
5. **Integridad**: Elimina inconsistencias entre Product.Stock y ProductStocks

## Consideraciones para el Frontend

- Actualizar componentes que consulten `Product.Stock` para usar `ProductStocks`
- Las APIs ahora devuelven `CurrentStock` calculado desde ProductStocks
- Validaciones de stock deben consultar ProductStocks por tienda

## Testing

Para probar la solución:

1. Aplicar migración de base de datos
2. Cargar productos desde Excel - verificar que se crean ProductStocks para todas las tiendas
3. Cargar stock inicial - verificar que solo actualiza ProductStocks de la tienda específica
4. Verificar que las ventas actualizan correctamente ProductStocks

## Fecha de Implementación
17 de Septiembre, 2025