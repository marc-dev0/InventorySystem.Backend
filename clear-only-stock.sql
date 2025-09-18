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


DO
$$
DECLARE
    r RECORD;
BEGIN
    FOR r IN (SELECT tablename FROM pg_tables WHERE schemaname = 'public')
    LOOP
        EXECUTE 'TRUNCATE TABLE public.' || quote_ident(r.tablename) || ' RESTART IDENTITY CASCADE;';
    END LOOP;
END;
$$;

INSERT INTO public."SystemConfigurations"
("Id", "ConfigKey", "ConfigValue", "ConfigType", "Description", "Category", "Active", "CreatedAt", "UpdatedAt", "CreatedBy", "UpdatedBy", "IsDeleted")
VALUES(6, 'SALES_GROUPING_COLUMN', 'SaleNumber', 'String', 'Columna por la cual agrupar las ventas (SaleNumber = columna F)', 'IMPORT', true, '2025-08-31 22:23:42.425', NULL, 'System', NULL, false);

INSERT INTO public."SystemConfigurations"
("Id", "ConfigKey", "ConfigValue", "ConfigType", "Description", "Category", "Active", "CreatedAt", "UpdatedAt", "CreatedBy", "UpdatedBy", "IsDeleted")
VALUES(7, 'STOCK_INITIAL_VALIDATION', 'true', 'Boolean', 'Validar que solo se permita una carga de stock inicial por tienda', 'IMPORT', true, '2025-08-31 22:23:48.148', NULL, 'System', NULL, false);
INSERT INTO public."SystemConfigurations"
("Id", "ConfigKey", "ConfigValue", "ConfigType", "Description", "Category", "Active", "CreatedAt", "UpdatedAt", "CreatedBy", "UpdatedBy", "IsDeleted")
VALUES(8, 'DEFAULT_STORE_CONFIG', '{"Code": "TIETAN", "Name": "Tienda Tantamayo", "Address": "Dirección Principal", "Phone": "123456789", "Active": true}', 'JSON', 'Configuración de tienda por defecto para el sistema', 'STORE', true, '2025-08-31 22:23:54.085', NULL, 'System', NULL, false);
INSERT INTO public."SystemConfigurations"
("Id", "ConfigKey", "ConfigValue", "ConfigType", "Description", "Category", "Active", "CreatedAt", "UpdatedAt", "CreatedBy", "UpdatedBy", "IsDeleted")
VALUES(2, 'IMPORT_COLUMNS_PRODUCT', '["Tienda","Código","Cod. barras","Nombre","Descripción","Categorias","Marca","Características","Impuestos","P. costo","Estado","Stock","Stock min","Ubicación","P. venta","Unidad","Nombre de lista de precio","Factor de conversión","Precio al por mayor","Cantidad mínima","Cantidad máxima"]', 'JSON', 'Columnas esperadas para importación de productos en Excel', 'IMPORT', true, '2025-08-31 22:23:15.703', NULL, 'System', NULL, false);
INSERT INTO public."SystemConfigurations"
("Id", "ConfigKey", "ConfigValue", "ConfigType", "Description", "Category", "Active", "CreatedAt", "UpdatedAt", "CreatedBy", "UpdatedBy", "IsDeleted")
VALUES(3, 'IMPORT_COLUMNS_STOCK', '["StoreCode","ProductCode","Stock"]', 'JSON', 'Columnas esperadas para importación de stock inicial en Excel', 'IMPORT', true, '2025-08-31 22:23:20.704', NULL, 'System', NULL, false);
INSERT INTO public."SystemConfigurations"
("Id", "ConfigKey", "ConfigValue", "ConfigType", "Description", "Category", "Active", "CreatedAt", "UpdatedAt", "CreatedBy", "UpdatedBy", "IsDeleted")
VALUES(4, 'IMPORT_MAPPING_STOCK', '{"StoreCode":0,"ProductCode":1,"Stock":2}', 'JSON', 'Mapeo de columnas para importación de stock (números de columna)', 'IMPORT', true, '2025-08-31 22:23:26.013', NULL, 'System', NULL, false);
INSERT INTO public."SystemConfigurations"
("Id", "ConfigKey", "ConfigValue", "ConfigType", "Description", "Category", "Active", "CreatedAt", "UpdatedAt", "CreatedBy", "UpdatedBy", "IsDeleted")
VALUES(5, 'IMPORT_COLUMNS_SALES', '["Raz\u00F3n Social","Empleado Venta","Almac\u00E9n","Cliente Nombre","Cliente Doc.","#-DOC","# Doc. Relacionado","Fecha","Hora","Tip. Doc.","Unidad","Cantidad","Precio de Venta","IGV","Total","Descuento aplicado (%)","Conversi\u00F3n","Moneda","Codigo SKU","Cod. alternativo","Marca","Categor\u00EDa","Caracter\u00EDsticas","Nombre","Descripci\u00F3n","Proveedor","Precio de costo","Empleado registro"]', 'JSON', 'Columnas esperadas para importación de ventas en Excel (agrupadas por Número de Venta columna F)', 'IMPORT', true, '2025-08-31 22:23:36.279', '2025-09-11 17:53:28.059', 'System', NULL, false);
INSERT INTO public."SystemConfigurations"
("Id", "ConfigKey", "ConfigValue", "ConfigType", "Description", "Category", "Active", "CreatedAt", "UpdatedAt", "CreatedBy", "UpdatedBy", "IsDeleted")
VALUES(1, 'IMPORT_MAPPING_SALES', '{"RazonSocialColumn":1,"EmpleadoVentaColumn":2,"AlmacenColumn":3,"ClienteNombreColumn":4,"ClienteDocColumn":5,"NumDocColumn":6,"DocRelacionadoColumn":7,"FechaColumn":8,"HoraColumn":9,"TipDocColumn":10,"UnidadColumn":11,"CantidadColumn":12,"PrecioVentaColumn":13,"IgvColumn":14,"TotalColumn":15,"DescuentoColumn":16,"ConversionColumn":17,"MonedaColumn":18,"CodigoSkuColumn":19,"CodAlternativoColumn":20,"MarcaColumn":21,"CategoriaColumn":22,"CaracteristicasColumn":23,"NombreColumn":24,"DescripcionColumn":25,"ProveedorColumn":26,"PrecioCostoColumn":27,"EmpleadoRegistroColumn":28,"StartRow":2}', 'Json', 'Mapeo de posiciones de columnas para importación de ventas', 'Import', true, '2025-09-11 17:53:28.059', NULL, NULL, NULL, false);
INSERT INTO public."SystemConfigurations"
("Id", "ConfigKey", "ConfigValue", "ConfigType", "Description", "Category", "Active", "CreatedAt", "UpdatedAt", "CreatedBy", "UpdatedBy", "IsDeleted")
VALUES(9, 'GLOBAL_MINIMUM_STOCK', '5', 'Number', 'Stock mínimo global para considerar productos con stock bajo', 'INVENTORY', true, '2025-09-15 10:52:08.339', NULL, 'Admin', NULL, false);
INSERT INTO public."Users"
("Id", "Username", "Email", "PasswordHash", "FirstName", "LastName", "Role", "IsActive", "CreatedAt", "UpdatedAt", "LastLoginAt")
VALUES(1, 'marc', 'm.rojascoraje@outlook.com', 'oeJUXe+QAt+SYCHjFuPN1vEN5B88fvZClpcDwwBypuw=', 'Miguel', 'Rojas', 'User', true, '2025-09-14 07:21:07.637', NULL, '2025-09-14 07:21:07.866');