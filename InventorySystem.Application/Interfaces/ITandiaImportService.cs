using InventorySystem.Application.DTOs;

namespace InventorySystem.Application.Interfaces;

public interface ITandiaImportService
{
    Task<BulkUploadResultDto> ImportProductsFromExcelAsync(Stream excelStream, string fileName);
    Task<BulkUploadResultDto> ImportSalesFromExcelAsync(Stream excelStream, string fileName, string storeCode);
    Task<TandiaUploadSummaryDto> ImportFullDatasetAsync(Stream productsStream, Stream salesStream);
    Task<List<TandiaProductDto>> ValidateProductsExcelAsync(Stream excelStream);
    Task<List<TandiaSaleDetailDto>> ValidateSalesExcelAsync(Stream excelStream);
    Task<ClearDataResultDto> ClearAllProductsAsync();
    Task<ClearDataResultDto> ClearAllSalesAsync();
    Task<int> DeleteProductsByBatchIdAsync(int batchId);
    Task<int> DeleteSalesByBatchIdAsync(int batchId);

    // New Tandia import types
    Task<BulkUploadResultDto> ImportCreditNotesFromExcelAsync(Stream excelStream, string fileName, string storeCode);
    Task<BulkUploadResultDto> ImportPurchasesFromExcelAsync(Stream excelStream, string fileName, string storeCode);
    Task<BulkUploadResultDto> ImportTransfersFromExcelAsync(Stream excelStream, string fileName, string originStoreCode, string destinationStoreCode);
    Task<List<TandiaCreditNoteDto>> ValidateCreditNotesExcelAsync(Stream excelStream);
    Task<List<TandiaPurchaseDto>> ValidatePurchasesExcelAsync(Stream excelStream);
    Task<List<TandiaTransferDto>> ValidateTransfersExcelAsync(Stream excelStream);
}