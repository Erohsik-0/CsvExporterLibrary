using Newtonsoft.Json;


namespace CsvExporterLibrary.Models
{
    /// <summary>
    /// Enhanced configuration options for CSV import/export operations
    /// </summary>
    public class CsvImportOptions
    {
        // CSV Format Options
        public char Delimiter { get; set; } = ',';
        public char QuoteChar { get; set; } = '"';
        public char EscapeChar { get; set; } = '\\';
        public string LineEnding { get; set; } = Environment.NewLine;

        // Processing Options
        public bool SkipEmptyRows { get; set; } = true;
        public bool SkipNullValues { get; set; } = false;
        public bool IgnoreParsingErrors { get; set; } = false;
        public bool TrimWhitespace { get; set; } = true;
        public string NullValueRepresentation { get; set; } = "";

        // Type Detection Options
        public bool AutoDetectTypes { get; set; } = true;
        public bool ParseNumbers { get; set; } = true;
        public bool ParseBooleans { get; set; } = true;
        public bool ParseDates { get; set; } = true;
        public bool ParseGuids { get; set; } = true;
        public bool EnableTypeCaching { get; set; } = true;

        // Number Handling
        public bool UseDecimalForNumbers { get; set; } = true;
        public string NumberFormat { get; set; } = "G";

        // Date Handling
        public bool ConvertDatesToStrings { get; set; } = false;
        public string DateFormat { get; set; } = "yyyy-MM-dd HH:mm:ss";
        public List<string> CustomDateFormats { get; set; } = new();

        // Value Preservation Options
        public bool PreserveNullValues { get; set; } = true;
        public bool PreserveGuidsAsStrings { get; set; } = true;
        public bool PreserveOriginalTypes { get; set; } = false;

        // CSV Writing Options
        public bool AlwaysQuoteStrings { get; set; } = false;
        public bool QuoteAllValues { get; set; } = false;

        // JSON Options
        public JsonSerializerSettings JsonSettings { get; set; } = new()
        {
            Formatting = Formatting.None,
            NullValueHandling = NullValueHandling.Include,
            DateFormatHandling = DateFormatHandling.IsoDateFormat
        };

        // Validation Options
        public int MaxRecordCount { get; set; } = 1_000_000;
        public int MaxFieldLength { get; set; } = 32_767; // Excel limit
        public bool ValidateFieldLengths { get; set; } = false;
    }
}
