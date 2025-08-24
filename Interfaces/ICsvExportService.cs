using CsvExporterLibrary.Models;
using CsvExporterLibrary.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CsvExporterLibrary.Interfaces
{
    /// <summary>
    /// Main interface for CSV export/import operations
    /// </summary>
    public interface ICsvExportService
    {
        // Export Operations
        Task<CsvExportResult> ExportToCsvAsync(CsvExportRequest request);
        Task<CsvExportResult> ExportFromJsonAsync(string jsonString, CsvExportOptions? options = null, string? fileName = null);
        Task<CsvExportResult> ExportFromObjectsAsync<T>(List<T> objects, CsvExportOptions? options = null, string? fileName = null);

        // Import Operations  
        Task<CsvImportResult> ImportFromCsvAsync(CsvImportRequest request);
        Task<CsvImportResult> ImportFromCsvBytesAsync(byte[] csvBytes, CsvImportOptions? options = null);
        Task<CsvImportResult> ImportFromCsvStreamAsync(Stream csvStream, CsvImportOptions? options = null);

        // Utility Operations
        Task<string> ConvertToJsonAsync(byte[] csvBytes, CsvImportOptions? options = null);
        Task<byte[]> ConvertJsonToCsvAsync(string jsonString, CsvExportOptions? options = null);
    }

}
