using CsvExporterLibrary.Models;


namespace CsvExporterLibrary.Interfaces
{
    /// <summary>
    /// Interface for data transformation operations
    /// </summary>
    public interface ICsvDataTransformer
    {
        List<Dictionary<string, object>> TransformForExport(List<Dictionary<string, object>> data, CsvExportOptions options);
        List<Dictionary<string, object>> TransformFromImport(List<Dictionary<string, object>> data, CsvImportOptions options);
        List<string> ExtractHeaders(List<Dictionary<string, object>> data, CsvExportOptions options);
        Dictionary<string, object> TransformRecord(Dictionary<string, object> record, CsvImportOptions options);
    }

}
