namespace InventorySystem.API.Utilities;

/// <summary>
/// Helper class for file validation operations
/// </summary>
public static class FileValidationHelper
{
    /// <summary>
    /// Validates if the uploaded file is a valid Excel file
    /// </summary>
    /// <param name="file">The file to validate</param>
    /// <returns>True if the file is a valid Excel file, false otherwise</returns>
    public static bool IsValidExcelFile(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return false;

        var allowedExtensions = new[] { ".xlsx", ".xls" };
        var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
        
        return allowedExtensions.Contains(fileExtension) && 
               (file.ContentType.Contains("spreadsheet") || 
                file.ContentType.Contains("excel"));
    }
}