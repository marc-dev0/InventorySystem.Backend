using InventorySystem.Application.DTOs;

namespace InventorySystem.Application.Interfaces;

public interface ITandiaImportService
{
    Task<BulkUploadResultDto> ImportProductsFromExcelAsync(Stream excelStream, string fileName);
    Task<BulkUploadResultDto> ImportSalesFromExcelAsync(Stream excelStream, string fileName);
    Task<TandiaUploadSummaryDto> ImportFullDatasetAsync(Stream productsStream, Stream salesStream);
    Task<List<TandiaProductDto>> ValidateProductsExcelAsync(Stream excelStream);
    Task<List<TandiaSaleDetailDto>> ValidateSalesExcelAsync(Stream excelStream);
}