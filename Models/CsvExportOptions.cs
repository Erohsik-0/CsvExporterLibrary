

namespace CsvExporterLibrary.Models
{
    /// <summary>
    /// Configuration options for CSV export operations
    /// </summary>
    public class CsvExportOptions : CsvImportOptions
    {
        public bool IncludeHeaders { get; set; } = true;
        public bool SortHeadersAlphabetically { get; set; } = true;
        public List<string>? CustomHeaderOrder { get; set; }
        public Dictionary<string, string>? HeaderMappings { get; set; }
    }

}
