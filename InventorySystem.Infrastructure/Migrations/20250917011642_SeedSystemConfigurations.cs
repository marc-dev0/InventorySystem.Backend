using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace InventorySystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SeedSystemConfigurations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2025, 9, 17, 1, 16, 41, 777, DateTimeKind.Utc).AddTicks(8033));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2025, 9, 17, 1, 16, 41, 777, DateTimeKind.Utc).AddTicks(9298));

            migrationBuilder.UpdateData(
                table: "Stores",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2025, 9, 17, 1, 16, 41, 779, DateTimeKind.Utc).AddTicks(2058));

            migrationBuilder.UpdateData(
                table: "Stores",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2025, 9, 17, 1, 16, 41, 779, DateTimeKind.Utc).AddTicks(3674));

            migrationBuilder.UpdateData(
                table: "Suppliers",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2025, 9, 17, 1, 16, 41, 778, DateTimeKind.Utc).AddTicks(9447));

            migrationBuilder.UpdateData(
                table: "Suppliers",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2025, 9, 17, 1, 16, 41, 779, DateTimeKind.Utc).AddTicks(573));

            migrationBuilder.InsertData(
                table: "SystemConfigurations",
                columns: new[] { "Id", "Active", "Category", "ConfigKey", "ConfigType", "ConfigValue", "CreatedAt", "CreatedBy", "Description", "IsDeleted", "UpdatedAt", "UpdatedBy" },
                values: new object[,]
                {
                    { 1, true, "INVENTORY", "GLOBAL_MINIMUM_STOCK", "Number", "5", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Stock mínimo global para considerar productos con stock bajo", false, null, null },
                    { 2, true, "IMPORT", "STOCK_INITIAL_VALIDATION", "Boolean", "true", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Validar que solo se permita una carga de stock inicial por tienda", false, null, null },
                    { 3, true, "IMPORT", "SALES_GROUPING_COLUMN", "String", "SaleNumber", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Columna por la cual agrupar las ventas (SaleNumber = columna F)", false, null, null },
                    { 4, true, "IMPORT", "IMPORT_COLUMNS_SALES", "JSON", "[\"Razón Social\",\"Empleado Venta\",\"Almacén\",\"Cliente Nombre\",\"Cliente Doc.\",\"#-DOC\",\"# Doc. Relacionado\",\"Fecha\",\"Hora\",\"Tip. Doc.\",\"Unidad\",\"Cantidad\",\"Precio de Venta\",\"IGV\",\"Total\",\"Descuento aplicado (%)\",\"Conversión\",\"Moneda\",\"Codigo SKU\",\"Cod. alternativo\",\"Marca\",\"Categoría\",\"Características\",\"Nombre\",\"Descripción\",\"Proveedor\",\"Precio de costo\",\"Empleado registro\"]", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Columnas esperadas para importación de ventas en Excel (agrupadas por Número de Venta columna F)", false, null, null },
                    { 5, true, "Import", "IMPORT_MAPPING_SALES", "Json", "{\"RazonSocialColumn\":1,\"EmpleadoVentaColumn\":2,\"AlmacenColumn\":3,\"ClienteNombreColumn\":4,\"ClienteDocColumn\":5,\"NumDocColumn\":6,\"DocRelacionadoColumn\":7,\"FechaColumn\":8,\"HoraColumn\":9,\"TipDocColumn\":10,\"UnidadColumn\":11,\"CantidadColumn\":12,\"PrecioVentaColumn\":13,\"IgvColumn\":14,\"TotalColumn\":15,\"DescuentoColumn\":16,\"ConversionColumn\":17,\"MonedaColumn\":18,\"CodigoSkuColumn\":19,\"CodAlternativoColumn\":20,\"MarcaColumn\":21,\"CategoriaColumn\":22,\"CaracteristicasColumn\":23,\"NombreColumn\":24,\"DescripcionColumn\":25,\"ProveedorColumn\":26,\"PrecioCostoColumn\":27,\"EmpleadoRegistroColumn\":28,\"StartRow\":2}", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Mapeo de posiciones de columnas para importación de ventas", false, null, null },
                    { 6, true, "IMPORT", "IMPORT_COLUMNS_PRODUCT", "JSON", "[\"Tienda\",\"Código\",\"Cod. barras\",\"Nombre\",\"Descripción\",\"Categorias\",\"Marca\",\"Características\",\"Impuestos\",\"P. costo\",\"Estado\",\"Stock\",\"Stock min\",\"Ubicación\",\"P. venta\",\"Unidad\",\"Nombre de lista de precio\",\"Factor de conversión\",\"Precio al por mayor\",\"Cantidad mínima\",\"Cantidad máxima\"]", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Columnas esperadas para importación de productos en Excel", false, null, null },
                    { 7, true, "IMPORT", "IMPORT_COLUMNS_STOCK", "JSON", "[\"StoreCode\",\"ProductCode\",\"Stock\"]", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Columnas esperadas para importación de stock inicial en Excel", false, null, null },
                    { 8, true, "IMPORT", "IMPORT_MAPPING_STOCK", "JSON", "{\"StoreCode\":0,\"ProductCode\":1,\"Stock\":2}", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Mapeo de columnas para importación de stock (números de columna)", false, null, null },
                    { 9, true, "STORE", "DEFAULT_STORE_CONFIG", "JSON", "{\"Code\": \"TIETAN\", \"Name\": \"Tienda Tantamayo\", \"Address\": \"Dirección Principal\", \"Phone\": \"123456789\", \"Active\": true}", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Configuración de tienda por defecto para el sistema", false, null, null }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "SystemConfigurations",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "SystemConfigurations",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "SystemConfigurations",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "SystemConfigurations",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "SystemConfigurations",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "SystemConfigurations",
                keyColumn: "Id",
                keyValue: 6);

            migrationBuilder.DeleteData(
                table: "SystemConfigurations",
                keyColumn: "Id",
                keyValue: 7);

            migrationBuilder.DeleteData(
                table: "SystemConfigurations",
                keyColumn: "Id",
                keyValue: 8);

            migrationBuilder.DeleteData(
                table: "SystemConfigurations",
                keyColumn: "Id",
                keyValue: 9);

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2025, 9, 12, 3, 57, 25, 732, DateTimeKind.Utc).AddTicks(4238));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2025, 9, 12, 3, 57, 25, 732, DateTimeKind.Utc).AddTicks(5840));

            migrationBuilder.UpdateData(
                table: "Stores",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2025, 9, 12, 3, 57, 25, 733, DateTimeKind.Utc).AddTicks(6719));

            migrationBuilder.UpdateData(
                table: "Stores",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2025, 9, 12, 3, 57, 25, 733, DateTimeKind.Utc).AddTicks(8407));

            migrationBuilder.UpdateData(
                table: "Suppliers",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2025, 9, 12, 3, 57, 25, 733, DateTimeKind.Utc).AddTicks(4637));

            migrationBuilder.UpdateData(
                table: "Suppliers",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2025, 9, 12, 3, 57, 25, 733, DateTimeKind.Utc).AddTicks(5516));

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "CreatedAt", "Email", "FirstName", "IsActive", "LastLoginAt", "LastName", "PasswordHash", "Role", "UpdatedAt", "Username" },
                values: new object[] { 1, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "admin@inventorysystem.com", "Administrator", true, null, "System", "zbFyeIKv6pKbhTL3XWaVhp5xzKF6oF8Kt7lEI8MEKy0=", "Admin", null, "admin" });
        }
    }
}
