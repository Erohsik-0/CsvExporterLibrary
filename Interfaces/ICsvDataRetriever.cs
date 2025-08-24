using CsvExporterLibrary.Models;

namespace CsvExporterLibrary.Interfaces
{
    /// <summary>
    /// Interface for CSV data retrieval operations
    /// </summary>
    public interface ICsvDataRetriever
    {
        Task<List<Dictionary<string, object>>> RetrieveFromJsonAsync(string jsonString);
        Task<List<Dictionary<string, object>>> RetrieveFromJsonAsync(byte[] jsonBytes);
        Task<List<Dictionary<string, object>>> RetrieveFromStreamAsync(Stream jsonStream);
        Task<List<Dictionary<string, object>>> RetrieveFromCsvAsync(byte[] csvBytes, CsvImportOptions options);
        Task<List<Dictionary<string, object>>> RetrieveFromCsvStreamAsync(Stream csvStream, CsvImportOptions options);
        Task<List<Dictionary<string, object>>> RetrieveFromCsvFileAsync(string filePath, CsvImportOptions options);
    }

}
