using CsvExporterLibrary.Exceptions;
using CsvExporterLibrary.Interfaces;
using CsvExporterLibrary.Models;

namespace CsvExporterLibrary.Extensions
{
    /// <summary>
    /// Extension methods for easier usage of the CSV export functionality
    /// </summary>
    public static class CsvExportExtensions
    {
        /// <summary>
        /// Converts a list of objects to CSV bytes
        /// </summary>
        public static async Task<byte[]> ToCsvBytesAsync<T>(this List<T>? objects, CsvExportOptions? options = null, ICsvExportService? service = null)
        {
            if (objects == null)
            {
                throw new ArgumentNullException(nameof(objects));
            }

            if (service == null)
            {
                throw new ArgumentNullException(nameof(service), "CSV Export Service must be provided");
            }

            var result = await service.ExportFromObjectsAsync(objects, options);
            return result.Data;
        }

        /// <summary>
        /// Converts CSV bytes to a list of dynamic objects
        /// </summary>
        public static async Task<List<Dictionary<string, object>>> FromCsvBytesAsync(this byte[]? csvBytes, CsvImportOptions? options = null, ICsvExportService? service = null)
        {
            if (csvBytes == null)
            {
                throw new ArgumentNullException(nameof(csvBytes));
            }

            if (service == null)
            {
                throw new ArgumentNullException(nameof(service), "CSV Export Service must be provided");
            }

            var result = await service.ImportFromCsvBytesAsync(csvBytes, options);
            return result.Data;
        }

        /// <summary>
        /// Converts a CsvExportResult to an HTTP response
        /// </summary>
        public static HttpResponseMessage ToHttpResponse(this CsvExportResult result)
        {
            return CsvExporterLibrary.Utilities.CsvUtilities.CreateCsvResponse(result);
        }

        /// <summary>
        /// Adds context information to a CsvExportException
        /// </summary>
        public static CsvExportException WithContext(this CsvExportException exception, string key, object value)
        {
            return exception.AddContext(key, value);
        }
    }

}
