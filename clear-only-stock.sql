-- Script para limpiar solo stock e inventario (mantiene productos, categorías, etc.)
-- Este script es más conservador, solo elimina datos de stock

-- 1. Eliminar movimientos de inventario
DELETE FROM "InventoryMovements";

-- 2. Eliminar stock de productos
DELETE FROM "ProductStocks";

-- 3. Resetear campo stock en Products (campo legacy)
UPDATE "Products" SET "Stock" = 0;

-- Reiniciar secuencias de IDs
ALTER SEQUENCE "ProductStocks_Id_seq" RESTART WITH 1;
ALTER SEQUENCE "InventoryMovements_Id_seq" RESTART WITH 1;

-- Verificar resultado
SELECT 'ProductStocks' as tabla, COUNT(*) as registros FROM "ProductStocks"
UNION ALL
SELECT 'InventoryMovements', COUNT(*) FROM "InventoryMovements"
UNION ALL
SELECT 'Products with Stock > 0', COUNT(*) FROM "Products" WHERE "Stock" > 0;