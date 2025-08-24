

namespace CsvExporterLibrary.Models
{
    /// <summary>
    /// Represents the result of a CSV export operation
    /// </summary>
    public class CsvExportResult
    {
        public byte[] Data { get; set; } = Array.Empty<byte>();
        public string FileName { get; set; } = string.Empty;
        public string ContentType { get; set; } = "text/csv";
        public int RecordCount { get; set; }
        public List<string> Headers { get; set; } = new();
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    }

}
