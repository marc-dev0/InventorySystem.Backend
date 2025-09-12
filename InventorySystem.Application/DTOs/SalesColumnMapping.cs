namespace InventorySystem.Application.DTOs;

public class SalesColumnMapping
{
    public int RazonSocialColumn { get; set; } = 1;       // A - Razón Social
    public int EmpleadoVentaColumn { get; set; } = 2;     // B - Empleado Venta
    public int AlmacenColumn { get; set; } = 3;           // C - Almacén
    public int ClienteNombreColumn { get; set; } = 4;     // D - Cliente Nombre
    public int ClienteDocColumn { get; set; } = 5;        // E - Cliente Doc.
    public int NumDocColumn { get; set; } = 6;            // F - #-DOC
    public int DocRelacionadoColumn { get; set; } = 7;    // G - # Doc. Relacionado
    public int FechaColumn { get; set; } = 8;             // H - Fecha
    public int HoraColumn { get; set; } = 9;              // I - Hora
    public int TipDocColumn { get; set; } = 10;           // J - Tip. Doc.
    public int UnidadColumn { get; set; } = 11;           // K - Unidad
    public int CantidadColumn { get; set; } = 12;         // L - Cantidad
    public int PrecioVentaColumn { get; set; } = 13;      // M - Precio de Venta
    public int IgvColumn { get; set; } = 14;              // N - IGV
    public int TotalColumn { get; set; } = 15;            // O - Total
    public int DescuentoColumn { get; set; } = 16;        // P - Descuento aplicado (%)
    public int ConversionColumn { get; set; } = 17;       // Q - Conversión
    public int MonedaColumn { get; set; } = 18;           // R - Moneda
    public int CodigoSkuColumn { get; set; } = 19;        // S - Codigo SKU
    public int CodAlternativoColumn { get; set; } = 20;   // T - Cod. alternativo
    public int MarcaColumn { get; set; } = 21;            // U - Marca
    public int CategoriaColumn { get; set; } = 22;        // V - Categoría
    public int CaracteristicasColumn { get; set; } = 23;  // W - Características
    public int NombreColumn { get; set; } = 24;           // X - Nombre
    public int DescripcionColumn { get; set; } = 25;      // Y - Descripción
    public int ProveedorColumn { get; set; } = 26;        // Z - Proveedor
    public int PrecioCostoColumn { get; set; } = 27;      // AA - Precio de costo
    public int EmpleadoRegistroColumn { get; set; } = 28; // AB - Empleado registro
    public int StartRow { get; set; } = 2;                // Fila 2 (después del header)
}