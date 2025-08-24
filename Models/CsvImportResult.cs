namespace CsvExporterLibrary.Models
{
    /// <summary>
    /// Represents the result of a CSV import operation
    /// </summary>
    public class CsvImportResult
    {
        public List<Dictionary<string, object>> Data { get; set; } = new();
        public List<string> Headers { get; set; } = new();
        public int RecordCount { get; set; }
        public DateTime ImportedAt { get; set; } = DateTime.UtcNow;
        public List<string> Warnings { get; set; } = new();
    }

}
