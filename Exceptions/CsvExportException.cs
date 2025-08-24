
namespace CsvExporterLibrary.Exceptions
{
    /// <summary>
    /// Base exception for CSV export/import operations
    /// </summary>
    public class CsvExportException : Exception
    {
        public string UserFriendlyMessage { get; }
        public string? OperationType { get; }
        public Dictionary<string, object> Context { get; }

        public CsvExportException(string message, string userFriendlyMessage, string? operationType = null)
            : base(message)
        {
            UserFriendlyMessage = userFriendlyMessage;
            OperationType = operationType;
            Context = new Dictionary<string, object>();
        }

        public CsvExportException(string message, string userFriendlyMessage, Exception innerException, string? operationType = null)
            : base(message, innerException)
        {
            UserFriendlyMessage = userFriendlyMessage;
            OperationType = operationType;
            Context = new Dictionary<string, object>();
        }

        public CsvExportException AddContext(string key, object value)
        {
            Context[key] = value;
            return this;
        }
    }

    /// <summary>
    /// Exception thrown when CSV data validation fails
    /// </summary>
    public class CsvValidationException : CsvExportException
    {
        public List<string> ValidationErrors { get; }

        public CsvValidationException(string message, List<string> validationErrors)
            : base(message, "The CSV data contains validation errors.", "Validation")
        {
            ValidationErrors = validationErrors;
        }
    }

    /// <summary>
    /// Exception thrown when CSV parsing fails
    /// </summary>
    public class CsvParsingException : CsvExportException
    {
        public int? LineNumber { get; }
        public string? ColumnName { get; }

        public CsvParsingException(string message, int? lineNumber = null, string? columnName = null)
            : base(message, "Unable to parse the CSV data. Please check the format.", "Parsing")
        {
            LineNumber = lineNumber;
            ColumnName = columnName;
        }
    }

}
