using CsvExporterLibrary.Models;


namespace CsvExporterLibrary.Interfaces
{
    /// <summary>
    /// Interface for CSV validation operations
    /// </summary>
    public interface ICsvValidator
    {
        Task<bool> ValidateExportDataAsync(List<Dictionary<string, object>>? data, CsvExportOptions options);
        Task<bool> ValidateImportDataAsync(byte[]? csvData, CsvImportOptions options);
        List<string> GetValidationErrors(List<Dictionary<string, object>>? data, CsvExportOptions options);
        List<string> GetCsvValidationErrors(byte[]? csvData, CsvImportOptions options);
    }

}
