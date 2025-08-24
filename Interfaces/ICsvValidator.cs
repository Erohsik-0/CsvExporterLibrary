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
