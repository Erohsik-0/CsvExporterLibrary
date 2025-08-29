using CsvExporterLibrary.Models;


namespace CsvExporterLibrary.Interfaces
{
    /// <summary>
    /// Interface for CSV file generation operations
    /// </summary>
    public interface ICsvFileGenerator
    {
        Task<byte[]> GenerateCsvAsync(List<Dictionary<string, object>> data, CsvExportOptions options);
        byte[] GenerateCsv(List<Dictionary<string, object>> data, CsvExportOptions options);
        Task<CsvExportResult> GenerateExportResultAsync(List<Dictionary<string, object>> data, CsvExportOptions options, string? fileName = null);
    }

}
