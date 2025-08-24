
namespace CsvExporterLibrary.Models
{
    /// <summary>
    /// Represents a CSV export request with configuration options
    /// </summary>
    public class CsvExportRequest
    {
        public List<Dictionary<string, object>>? JsonData { get; set; }
        public CsvExportOptions Options { get; set; } = new();
        public string? FileName { get; set; } = "export.csv";
    }

}
