using CsvExporterLibrary.Models;

namespace CsvExporterLibrary.Utilities
{
    /// <summary>
    /// Utility methods for common CSV operations
    /// </summary>
    public static class CsvUtilities
    {
        /// <summary>
        /// Creates a memory stream from CSV bytes for HTTP responses
        /// </summary>
        public static MemoryStream CreateCsvStream(byte[] csvBytes)
        {
            if (csvBytes == null)
            {
                throw new ArgumentNullException(nameof(csvBytes));
            }

            return new MemoryStream(csvBytes);
        }

        /// <summary>
        /// Gets CSV content type for HTTP responses
        /// </summary>
        public static string GetCsvContentType()
        {
            return "text/csv; charset=utf-8";
        }

        /// <summary>
        /// Creates CSV download headers for HTTP responses
        /// </summary>
        public static Dictionary<string, string> CreateCsvDownloadHeaders(string filename = "data.csv")
        {
            return new Dictionary<string, string>
            {
                ["Content-Type"] = "text/csv; charset=utf-8",
                ["Content-Disposition"] = $"attachment; filename=\"{filename}\"",
                ["Cache-Control"] = "no-cache, no-store, must-revalidate",
                ["Pragma"] = "no-cache",
                ["Expires"] = "0"
            };
        }

        /// <summary>
        /// Creates a configured HttpResponseMessage for CSV download
        /// </summary>
        public static HttpResponseMessage CreateCsvResponse(CsvExportResult result)
        {
            var response = new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(result.Data)
            };

            response.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/csv")
            {
                CharSet = "utf-8"
            };

            response.Content.Headers.ContentDisposition = new System.Net.Http.Headers.ContentDispositionHeaderValue("attachment")
            {
                FileName = result.FileName
            };

            return response;
        }

        /// <summary>
        /// Validates CSV file extension
        /// </summary>
        public static bool IsValidCsvFileName(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                return false;

            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            return extension == ".csv";
        }

        /// <summary>
        /// Sanitizes a filename for CSV export
        /// </summary>
        public static string SanitizeFileName(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                return "export.csv";

            var invalidChars = Path.GetInvalidFileNameChars();
            var sanitized = new string(fileName.Where(c => !invalidChars.Contains(c)).ToArray());

            if (string.IsNullOrWhiteSpace(sanitized))
                sanitized = "export";

            if (!sanitized.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
                sanitized += ".csv";

            return sanitized;
        }
    }

}
