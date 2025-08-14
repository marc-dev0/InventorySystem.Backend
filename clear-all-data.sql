-- Script para limpiar toda la base de datos de productos y stock
-- CUIDADO: Este script eliminará TODOS los datos relacionados con productos

-- Deshabilitar foreign key checks temporalmente (si fuera necesario)
-- SET foreign_key_checks = 0;

-- 1. Eliminar movimientos de inventario (depende de ProductStocks)
DELETE FROM "InventoryMovements";

-- 2. Eliminar detalles de ventas (depende de Products)
DELETE FROM "SaleDetails";

-- 3. Eliminar detalles de compras (depende de Products)
DELETE FROM "PurchaseDetails";

-- 4. Eliminar ventas (ahora que no tienen detalles)
DELETE FROM "Sales";

-- 5. Eliminar compras (ahora que no tienen detalles)
DELETE FROM "Purchases";

-- 6. Eliminar stock de productos (depende de Products y Stores)
DELETE FROM "ProductStocks";

-- 7. Eliminar productos (ahora que no tienen referencias)
DELETE FROM "Products";

-- 8. Eliminar categorías (ahora que no tienen productos)
DELETE FROM "Categories";

-- 9. Eliminar proveedores (ahora que no tienen productos)
DELETE FROM "Suppliers";

-- 10. Eliminar clientes (ahora que no tienen ventas)
DELETE FROM "Customers";

-- Reiniciar secuencias de IDs (opcional)
ALTER SEQUENCE "Categories_Id_seq" RESTART WITH 1;
ALTER SEQUENCE "Suppliers_Id_seq" RESTART WITH 1;
ALTER SEQUENCE "Products_Id_seq" RESTART WITH 1;
ALTER SEQUENCE "Customers_Id_seq" RESTART WITH 1;
ALTER SEQUENCE "Sales_Id_seq" RESTART WITH 1;
ALTER SEQUENCE "SaleDetails_Id_seq" RESTART WITH 1;
ALTER SEQUENCE "Purchases_Id_seq" RESTART WITH 1;
ALTER SEQUENCE "PurchaseDetails_Id_seq" RESTART WITH 1;
ALTER SEQUENCE "ProductStocks_Id_seq" RESTART WITH 1;
ALTER SEQUENCE "InventoryMovements_Id_seq" RESTART WITH 1;

-- Verificar que las tablas están vacías
SELECT 'Categories' as tabla, COUNT(*) as registros FROM "Categories"
UNION ALL
SELECT 'Suppliers', COUNT(*) FROM "Suppliers"
UNION ALL
SELECT 'Products', COUNT(*) FROM "Products"
UNION ALL
SELECT 'Customers', COUNT(*) FROM "Customers"
UNION ALL
SELECT 'Sales', COUNT(*) FROM "Sales"
UNION ALL
SELECT 'SaleDetails', COUNT(*) FROM "SaleDetails"
UNION ALL
SELECT 'Purchases', COUNT(*) FROM "Purchases"
UNION ALL
SELECT 'PurchaseDetails', COUNT(*) FROM "PurchaseDetails"
UNION ALL
SELECT 'ProductStocks', COUNT(*) FROM "ProductStocks"
UNION ALL
SELECT 'InventoryMovements', COUNT(*) FROM "InventoryMovements";

-- Verificar que los Stores se mantienen intactos
SELECT 'Stores (should remain)', COUNT(*) FROM "Stores";