using CsvExporterLibrary.Exceptions;
using CsvExporterLibrary.Interfaces;
using Newtonsoft.Json;
using CsvExporterLibrary.Models;

namespace CsvExporterLibrary.Services
{
    /// <summary>
    /// Service responsible for retrieving data from various sources
    /// </summary>
    public class CsvDataRetriever : ICsvDataRetriever
    {
        private readonly ICsvValidator _validator;

        public CsvDataRetriever(ICsvValidator validator)
        {
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        }

        public async Task<List<Dictionary<string, object>>> RetrieveFromJsonAsync(string jsonString)
        {
            if (string.IsNullOrWhiteSpace(jsonString))
            {
                throw new CsvExportException(
                    "JSON string is null or empty",
                    "No data provided for conversion",
                    "JsonRetrieval");
            }

            try
            {
                return await Task.Run(() =>
                {
                    var data = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(jsonString);
                    return data ?? new List<Dictionary<string, object>>();
                });
            }
            catch (JsonException ex)
            {
                throw new CsvExportException(
                    $"Failed to parse JSON data: {ex.Message}",
                    "Invalid JSON format. Please check your data.",
                    ex,
                    "JsonRetrieval");
            }
        }

        public async Task<List<Dictionary<string, object>>> RetrieveFromJsonAsync(byte[] jsonBytes)
        {
            if (jsonBytes == null || jsonBytes.Length == 0)
            {
                throw new CsvExportException(
                    "JSON bytes are null or empty",
                    "No data provided for conversion",
                    "JsonRetrieval");
            }

            try
            {
                var jsonString = System.Text.Encoding.UTF8.GetString(jsonBytes);
                return await RetrieveFromJsonAsync(jsonString);
            }
            catch (Exception ex) when (!(ex is CsvExportException))
            {
                throw new CsvExportException(
                    $"Failed to convert JSON bytes to string: {ex.Message}",
                    "Unable to process the provided data",
                    ex,
                    "JsonRetrieval");
            }
        }

        public async Task<List<Dictionary<string, object>>> RetrieveFromStreamAsync(Stream jsonStream)
        {
            if (jsonStream == null)
            {
                throw new CsvExportException(
                    "JSON stream is null",
                    "No data stream provided",
                    "JsonRetrieval");
            }

            try
            {
                using var reader = new StreamReader(jsonStream, System.Text.Encoding.UTF8);
                var jsonString = await reader.ReadToEndAsync();
                return await RetrieveFromJsonAsync(jsonString);
            }
            catch (Exception ex) when (!(ex is CsvExportException))
            {
                throw new CsvExportException(
                    $"Failed to read from JSON stream: {ex.Message}",
                    "Unable to read the provided data stream",
                    ex,
                    "JsonRetrieval");
            }
        }

        public async Task<List<Dictionary<string, object>>> RetrieveFromCsvAsync(byte[] csvBytes, CsvImportOptions options)
        {
            await _validator.ValidateImportDataAsync(csvBytes, options);

            try
            {
                using var memoryStream = new MemoryStream(csvBytes);
                return await RetrieveFromCsvStreamAsync(memoryStream, options);
            }
            catch (Exception ex) when (!(ex is CsvExportException))
            {
                throw new CsvExportException(
                    $"Failed to process CSV bytes: {ex.Message}",
                    "Unable to process the CSV data",
                    ex,
                    "CsvRetrieval");
            }
        }

        public async Task<List<Dictionary<string, object>>> RetrieveFromCsvStreamAsync(Stream csvStream, CsvImportOptions options)
        {
            if (csvStream == null)
            {
                throw new CsvExportException(
                    "CSV stream is null",
                    "No CSV data stream provided",
                    "CsvRetrieval");
            }

            try
            {
                return await Task.Run(() =>
                {
                    using var reader = new StreamReader(csvStream, System.Text.Encoding.UTF8);
                    return ParseCsvFromReader(reader, options);
                });
            }
            catch (Exception ex) when (!(ex is CsvExportException))
            {
                throw new CsvExportException(
                    $"Failed to read CSV stream: {ex.Message}",
                    "Unable to read the CSV data",
                    ex,
                    "CsvRetrieval");
            }
        }

        public async Task<List<Dictionary<string, object>>> RetrieveFromCsvFileAsync(string filePath, CsvImportOptions options)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new CsvExportException(
                    "File path is null or empty",
                    "No file path provided",
                    "CsvRetrieval");
            }

            if (!File.Exists(filePath))
            {
                throw new CsvExportException(
                    $"File not found: {filePath}",
                    "The specified file could not be found",
                    "CsvRetrieval");
            }

            try
            {
                var bytes = await File.ReadAllBytesAsync(filePath);
                return await RetrieveFromCsvAsync(bytes, options);
            }
            catch (Exception ex) when (!(ex is CsvExportException))
            {
                throw new CsvExportException(
                    $"Failed to read CSV file: {ex.Message}",
                    "Unable to read the CSV file",
                    ex,
                    "CsvRetrieval");
            }
        }

        private List<Dictionary<string, object>> ParseCsvFromReader(StreamReader reader, CsvImportOptions options)
        {
            var result = new List<Dictionary<string, object>>();
            var lineNumber = 0;

            // Read headers
            var headerLine = reader.ReadLine();
            lineNumber++;

            if (string.IsNullOrWhiteSpace(headerLine))
            {
                return result;
            }

            var headers = ParseCsvLine(headerLine, options);
            if (headers.Count == 0)
            {
                throw new CsvParsingException("No headers found in CSV data", lineNumber);
            }

            // Read data lines
            string? line;
            while ((line = reader.ReadLine()) != null)
            {
                lineNumber++;

                if (string.IsNullOrWhiteSpace(line) && options.SkipEmptyRows)
                {
                    continue;
                }

                try
                {
                    var values = ParseCsvLine(line, options);
                    var record = CreateRecord(headers, values, options, lineNumber);

                    if (record.Count > 0 || !options.SkipEmptyRows)
                    {
                        result.Add(record);
                    }
                }
                catch (Exception ex)
                {
                    if (!options.IgnoreParsingErrors)
                    {
                        throw new CsvParsingException($"Error parsing line {lineNumber}: {ex.Message}", lineNumber);
                    }
                }
            }

            return result;
        }

        private List<string> ParseCsvLine(string line, CsvImportOptions options)
        {
            var values = new List<string>();
            var currentValue = new System.Text.StringBuilder();
            bool inQuotes = false;
            bool escapeNext = false;

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];

                if (escapeNext)
                {
                    currentValue.Append(c);
                    escapeNext = false;
                    continue;
                }

                switch (c)
                {
                    case '"' when options.QuoteChar == '"':
                        if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                        {
                            currentValue.Append('"');
                            i++;
                        }
                        else
                        {
                            inQuotes = !inQuotes;
                        }
                        break;

                    case '\\' when options.EscapeChar == '\\':
                        escapeNext = true;
                        break;

                    case ',' when c == options.Delimiter && !inQuotes:
                        values.Add(options.TrimWhitespace ? currentValue.ToString().Trim() : currentValue.ToString());
                        currentValue.Clear();
                        break;

                    default:
                        currentValue.Append(c);
                        break;
                }
            }

            values.Add(options.TrimWhitespace ? currentValue.ToString().Trim() : currentValue.ToString());
            return values;
        }

        private Dictionary<string, object> CreateRecord(List<string> headers, List<string> values, CsvImportOptions options, int lineNumber)
        {
            var record = new Dictionary<string, object>();

            for (int i = 0; i < headers.Count; i++)
            {
                var header = headers[i];
                string value = i < values.Count ? values[i] : "";

                if (string.IsNullOrWhiteSpace(value))
                {
                    if (!options.SkipNullValues)
                    {
                        record[header] = options.PreserveNullValues ? null! : "";
                    }
                    continue;
                }

                // Remove quotes if present
                if (value.StartsWith(options.QuoteChar.ToString()) &&
                    value.EndsWith(options.QuoteChar.ToString()) &&
                    value.Length > 1)
                {
                    value = value[1..^1];
                    value = value.Replace($"{options.QuoteChar}{options.QuoteChar}", options.QuoteChar.ToString());
                }

                record[header] = value;
            }

            return record;
        }
    }
}
