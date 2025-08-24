using System.ComponentModel.DataAnnotations;


namespace CsvExporterLibrary.Models
{
    /// <summary>
    /// Represents a CSV import request with configuration options
    /// </summary>
    public class CsvImportRequest
    {
        [Required]
        public byte[]? CsvData { get; set; }
        public CsvImportOptions Options { get; set; } = new();
    }

}
