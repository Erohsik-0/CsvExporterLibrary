using CsvExporterLibrary.Interfaces;
using CsvExporterLibrary.Models;
using CsvExporterLibrary.Exceptions;
using Newtonsoft.Json;



namespace CsvExporterLibrary.Services
{

    public class CsvExportService : ICsvExportService
    {

        private readonly ICsvDataRetriever _dataRetriever;
        private readonly ICsvFileGenerator _fileGenerator;
        private readonly ICsvDataTransformer _dataTransformer;
        private readonly ICsvValidator _validator;

        public CsvExportService(ICsvDataRetriever dataRetriever, ICsvFileGenerator fileGenerator, ICsvDataTransformer dataTransformer, ICsvValidator validator)
        {
            _dataRetriever = dataRetriever ?? throw new ArgumentNullException(nameof(dataRetriever));
            _fileGenerator = fileGenerator ?? throw new ArgumentNullException(nameof(fileGenerator));
            _dataTransformer = dataTransformer ?? throw new ArgumentNullException(nameof(dataTransformer));
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        }


        public async Task<CsvExportResult> ExportToCsvAsync(CsvExportRequest request)
        {
            if (request == null)
            {
                throw new CsvExportException("Export request cannot be null", "Invalid export request", "Export");
            }

            if (request.JsonData == null || request.JsonData.Count == 0)
            {
                throw new CsvExportException("No data provided for export", "No data available for export", "Export");
            }

            try
            {
                await _validator.ValidateExportDataAsync(request.JsonData, request.Options);
                return await _fileGenerator.GenerateExportResultAsync(request.JsonData, request.Options, request.FileName);
            }
            catch (Exception ex) when (!(ex is CsvExportException))
            {
                throw new CsvExportException($"Export operation failed : {ex.Message}", "Unable to complete the export operation", ex, "Export");
            }
        }

        public async Task<CsvExportResult> ExportFromJsonAsync(string jsonString, CsvExportOptions? options = null, string? fileName = null)
        {
            try
            {
                var data = await _dataRetriever.RetrieveFromJsonAsync(jsonString);
                var exportOptions = options ?? new CsvExportOptions();

                var request = new CsvExportRequest
                {
                    JsonData = data,
                    Options = exportOptions,
                    FileName = fileName
                };

                return await ExportToCsvAsync(request);
            }
            catch (Exception ex) when (!(ex is CsvExportException))
            {
                throw new CsvExportException($"Json export failed : {ex.Message}", "Unable to export Json data to CSV", ex, "JsonExport");
            }
        }


        public async Task<CsvExportResult> ExportFromObjectsAsync<T>(List<T> objects, CsvExportOptions? options = null, string? fileName = null)
        {

            if (objects == null)
            {
                throw new CsvExportException("Objects list cannot be null", "No data provided for export", "ObjectExport");
            }

            try
            {
                var jsonData = objects.Select(obj =>
                {
                    if (obj == null) return new Dictionary<string, object>();

                    var json = JsonConvert.SerializeObject(obj, options?.JsonSettings ?? new JsonSerializerSettings());
                    return JsonConvert.DeserializeObject<Dictionary<string, object>>(json) ?? new Dictionary<string, object>();
                }).ToList();

                var exportOptions = options ?? new CsvExportOptions();

                var request = new CsvExportRequest
                {
                    JsonData = jsonData,
                    Options = exportOptions,
                    FileName = fileName
                };

                return await ExportToCsvAsync(request);
            }
            catch (Exception ex) when (!(ex is CsvExportException))
            {
                throw new CsvExportException($"Object export failed : {ex.Message}", "Unable to export objects to CSV", ex, "ObjectExport");
            }

        }


        public async Task<CsvImportResult> ImportFromCsvAsync(CsvImportRequest request)
        {

            if (request == null)
            {
                throw new CsvExportException("Import request cannot be null", "Invalid import request", "Import");
            }

            if (request.CsvData == null || request.CsvData.Length == 0)
            {
                throw new CsvExportException("No CSV data provided for import", "No data available for import", "Import");
            }

            try
            {
                await _validator.ValidateImportDataAsync(request.CsvData, request.Options);

                var rawData = await _dataRetriever.RetrieveFromCsvAsync(request.CsvData, request.Options);
                var transformedData = _dataTransformer.TransformFromImport(rawData, request.Options);
                var headers = _dataTransformer.ExtractHeaders(transformedData, new CsvExportOptions());

                return new CsvImportResult
                {
                    Data = transformedData,
                    Headers = headers,
                    RecordCount = transformedData.Count,
                    ImportedAt = DateTime.UtcNow,
                    Warnings = new List<string>()
                };
            }
            catch (Exception ex) when (!(ex is CsvExportException))
            {
                throw new CsvExportException($"Import operation failed : {ex.Message}", "Unable to complete the import operation", ex, "Import");
            }
        }


        public async Task<CsvImportResult> ImportFromCsvBytesAsync(byte[] csvBytes, CsvImportOptions? options = null)
        {
            var importOptions = options ?? new CsvImportOptions();

            var request = new CsvImportRequest
            {
                CsvData = csvBytes,
                Options = importOptions
            };

            return await ImportFromCsvAsync(request);
        }

        public async Task<CsvImportResult> ImportFromCsvStreamAsync(Stream csvStream, CsvImportOptions? options = null)
        {

            if (csvStream == null)
            {
                throw new CsvExportException("CSV stream cannot be null", "No Csv Stream provided", "StreamImport");
            }

            try
            {

                var importOptions = options ?? new CsvImportOptions();
                var data = await _dataRetriever.RetrieveFromCsvStreamAsync(csvStream, importOptions);

                var transformedData = _dataTransformer.TransformFromImport(data, importOptions);
                var headers = _dataTransformer.ExtractHeaders(transformedData, new CsvExportOptions());

                return new CsvImportResult
                {
                    Data = transformedData,
                    Headers = headers,
                    RecordCount = transformedData.Count,
                    ImportedAt = DateTime.UtcNow,
                    Warnings = new List<string>()
                };
            }
            catch (Exception ex) when (!(ex is CsvExportException))
            {
                throw new CsvExportException($"Stream import failed : {ex.Message}", "Unable to import CSV from stream", ex, "StreamImport");
            }
        }


        public async Task<string> ConvertToJsonAsync(byte[] csvBytes, CsvImportOptions? options = null)
        {

            try
            {
                var importResult = await ImportFromCsvBytesAsync(csvBytes, options);
                var jsonSettings = options?.JsonSettings ?? new JsonSerializerSettings { Formatting = Formatting.Indented };
                return JsonConvert.SerializeObject(importResult.Data, jsonSettings);
            }
            catch (Exception ex) when (!(ex is CsvExportException))
            {
                throw new CsvExportException($"CSV to JSON conversion failed : {ex.Message}", "Unable to convert CSV to JSON", ex, "CsvToJsonConversion");
            }
        }


        public async Task<byte[]> ConvertJsonToCsvAsync(string jsonString , CsvExportOptions? options =  null)
        {

            try
            {
                var exportResult = await ExportFromJsonAsync(jsonString , options);
                return exportResult.Data;
            }
            catch(Exception ex) when (!(ex is CsvExportException))
            {
                throw new CsvExportException($"JSON to CSV conversion failed : {ex.Message}", "Unable to convert JSON to CSV", ex, "JsonToCsvConversion");
            }

        }
    }

}

//namespace CsvExporterLibrary.Services
//{
//    public class CsvExporterService
//    {
//        private readonly CsvImportOptions _options;
//        private readonly TypeDetector _typeDetector;
//        private readonly CsvWriter _csvWriter;

//        public CsvExporterService(CsvImportOptions? options = null)
//        {
//            _options = options ?? new CsvImportOptions();
//            _typeDetector = new TypeDetector(_options);
//            _csvWriter = new CsvWriter();
//        }

//        #region JSON to CSV Conversion

//        /// <summary>
//        /// Converts JSON data to CSV bytes asynchronously
//        /// </summary>
//        public async Task<byte[]> ConvertJsonToCsvAsync(List<Dictionary<string, object>>? jsonData)
//        {
//            if (jsonData == null)
//            {
//                throw new ArgumentNullException(nameof(jsonData), "JSON data cannot be null");
//            }
//            return await Task.Run(() => ConvertJsonToCsv(jsonData));
//        }

//        /// <summary>
//        /// Converts JSON data to CSV bytes synchronously
//        /// </summary>
//        public byte[] ConvertJsonToCsv(List<Dictionary<string, object>>? jsonData)
//        {
//            if (jsonData == null || jsonData.Count == 0)
//            {
//                throw new ArgumentException("JSON data cannot be null or empty", nameof(jsonData));
//            }

//            try
//            {
//                using var memoryStream = new MemoryStream();
//                using var writer = new StreamWriter(memoryStream, Encoding.UTF8);

//                // Extract all unique headers from all records
//                var allHeaders = ExtractAllHeaders(jsonData);

//                // Write headers
//                _csvWriter.WriteHeaders(writer, allHeaders);

//                // Write data rows
//                foreach (var record in jsonData)
//                {
//                    _csvWriter.WriteDataRow(writer, record, allHeaders, _options);
//                }

//                writer.Flush();
//                return memoryStream.ToArray();
//            }
//            catch (Exception ex)
//            {
//                throw new InvalidOperationException($"JSON to CSV conversion failed: {ex.Message}", ex);
//            }
//        }

//        /// <summary>
//        /// Converts JSON string to CSV bytes
//        /// </summary>
//        public async Task<byte[]> ConvertJsonStringToCsvAsync(string jsonString)
//        {
//            if (string.IsNullOrEmpty(jsonString))
//            {
//                throw new ArgumentException("JSON string cannot be null or empty", nameof(jsonString));
//            }

//            var jsonData = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(jsonString);
//            return await ConvertJsonToCsvAsync(jsonData);
//        }

//        /// <summary>
//        /// Converts JSON bytes to CSV bytes
//        /// </summary>
//        public async Task<byte[]> ConvertJsonBytesToCsvAsync(byte[] jsonBytes)
//        {
//            if (jsonBytes == null)
//            {
//                throw new ArgumentNullException(nameof(jsonBytes));
//            }

//            var jsonString = Encoding.UTF8.GetString(jsonBytes);
//            return await ConvertJsonStringToCsvAsync(jsonString);
//        }

//        /// <summary>
//        /// Converts JSON from stream to CSV bytes
//        /// </summary>
//        public async Task<byte[]> ConvertJsonStreamToCsvAsync(Stream jsonStream)
//        {
//            if (jsonStream == null)
//            {
//                throw new ArgumentNullException(nameof(jsonStream));
//            }

//            using var reader = new StreamReader(jsonStream, Encoding.UTF8);
//            var jsonString = await reader.ReadToEndAsync();
//            return await ConvertJsonStringToCsvAsync(jsonString);
//        }

//        #endregion

//        #region CSV to JSON Conversion

//        /// <summary>
//        /// Converts CSV bytes to JSON data asynchronously
//        /// </summary>
//        public async Task<List<Dictionary<string, object>>> ConvertCsvToJsonAsync(byte[] csvBytes)
//        {
//            return await Task.Run(() => ConvertCsvToJson(csvBytes));
//        }

//        /// <summary>
//        /// Converts CSV bytes to JSON data synchronously
//        /// </summary>
//        public List<Dictionary<string, object>> ConvertCsvToJson(byte[] csvBytes)
//        {
//            if (csvBytes == null || csvBytes.Length == 0)
//            {
//                throw new ArgumentException("CSV data cannot be null or empty", nameof(csvBytes));
//            }

//            try
//            {
//                using var memoryStream = new MemoryStream(csvBytes);
//                using var reader = new StreamReader(memoryStream, Encoding.UTF8);

//                return ParseCsvToJson(reader);
//            }
//            catch (Exception ex)
//            {
//                throw new InvalidOperationException($"CSV to JSON conversion failed: {ex.Message}", ex);
//            }
//        }

//        /// <summary>
//        /// Converts CSV from stream to JSON data
//        /// </summary>
//        public async Task<List<Dictionary<string, object>>> ConvertCsvStreamToJsonAsync(Stream csvStream)
//        {
//            if (csvStream == null)
//            {
//                throw new ArgumentNullException(nameof(csvStream));
//            }

//            using var memoryStream = new MemoryStream();
//            await csvStream.CopyToAsync(memoryStream);
//            return await ConvertCsvToJsonAsync(memoryStream.ToArray());
//        }

//        /// <summary>
//        /// Converts CSV file to JSON data
//        /// </summary>
//        public async Task<List<Dictionary<string, object>>> ConvertCsvFileToJsonAsync(string filePath)
//        {
//            if (string.IsNullOrEmpty(filePath))
//            {
//                throw new ArgumentException("File path cannot be null or empty", nameof(filePath));
//            }

//            var bytes = await File.ReadAllBytesAsync(filePath);
//            return await ConvertCsvToJsonAsync(bytes);
//        }

//        /// <summary>
//        /// Converts CSV to JSON string
//        /// </summary>
//        public async Task<string> ConvertCsvToJsonStringAsync(byte[] csvBytes)
//        {
//            var jsonData = await ConvertCsvToJsonAsync(csvBytes);
//            return JsonConvert.SerializeObject(jsonData, _options.JsonFormatting);
//        }

//        #endregion

//        #region Private Methods - JSON to CSV

//        private HashSet<string> ExtractAllHeaders(List<Dictionary<string, object>> jsonData)
//        {
//            var headers = new HashSet<string>();

//            foreach (var record in jsonData)
//            {
//                if (record != null)
//                {
//                    foreach (var key in record.Keys)
//                    {
//                        if (!_options.SkipNullValues || record[key] != null)
//                        {
//                            headers.Add(key);
//                        }
//                    }
//                }
//            }

//            return headers;
//        }

//        #endregion

//        #region Private Methods - CSV to JSON

//        private List<Dictionary<string, object>> ParseCsvToJson(StreamReader reader)
//        {
//            var result = new List<Dictionary<string, object>>();

//            // Read and parse headers
//            var headerLine = reader.ReadLine();
//            if (string.IsNullOrWhiteSpace(headerLine))
//            {
//                return result;
//            }

//            var headers = ParseCsvLine(headerLine);
//            if (headers.Count == 0)
//            {
//                return result;
//            }

//            // Read data lines
//            string? line;
//            int lineNumber = 1;

//            while ((line = reader.ReadLine()) != null)
//            {
//                lineNumber++;

//                if (string.IsNullOrWhiteSpace(line) && _options.SkipEmptyRows)
//                {
//                    continue;
//                }

//                try
//                {
//                    var values = ParseCsvLine(line);
//                    var record = CreateJsonRecord(headers, values, lineNumber);

//                    if (record.Count > 0 || !_options.SkipEmptyRows)
//                    {
//                        result.Add(record);
//                    }
//                }
//                catch (Exception ex)
//                {
//                    if (!_options.IgnoreParsingErrors)
//                    {
//                        throw new InvalidOperationException($"Error parsing CSV line {lineNumber}: {ex.Message}", ex);
//                    }
//                }
//            }

//            return result;
//        }

//        private List<string> ParseCsvLine(string line)
//        {
//            var values = new List<string>();
//            var currentValue = new StringBuilder();
//            bool inQuotes = false;
//            bool escapeNext = false;

//            for (int i = 0; i < line.Length; i++)
//            {
//                char c = line[i];

//                if (escapeNext)
//                {
//                    currentValue.Append(c);
//                    escapeNext = false;
//                    continue;
//                }

//                switch (c)
//                {
//                    case '"' when _options.QuoteChar == '"':
//                        if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
//                        {
//                            // Escaped quote
//                            currentValue.Append('"');
//                            i++; // Skip next quote
//                        }
//                        else
//                        {
//                            inQuotes = !inQuotes;
//                        }
//                        break;

//                    case '\\' when _options.EscapeChar == '\\':
//                        escapeNext = true;
//                        break;

//                    case ',' when c == _options.Delimiter && !inQuotes:
//                        values.Add(currentValue.ToString());
//                        currentValue.Clear();
//                        break;

//                    default:
//                        currentValue.Append(c);
//                        break;
//                }
//            }

//            // Add the last value
//            values.Add(currentValue.ToString());

//            return values;
//        }

//        private Dictionary<string, object> CreateJsonRecord(List<string> headers, List<string> values, int lineNumber)
//        {
//            var record = new Dictionary<string, object>();

//            for (int i = 0; i < headers.Count; i++)
//            {
//                var header = headers[i];
//                string value = i < values.Count ? values[i] : "";

//                // Handle empty/null values
//                if (string.IsNullOrWhiteSpace(value))
//                {
//                    if (!_options.SkipNullValues)
//                    {
//                        record[header] = _options.PreserveNullValues ? null! : "";
//                    }
//                    continue;
//                }

//                // Remove quotes if they exist
//                if (value.StartsWith(_options.QuoteChar.ToString()) && value.EndsWith(_options.QuoteChar.ToString()) && value.Length > 1)
//                {
//                    value = value.Substring(1, value.Length - 2);
//                    // Handle escaped quotes
//                    value = value.Replace($"{_options.QuoteChar}{_options.QuoteChar}", _options.QuoteChar.ToString());
//                }

//                // Convert value using type detector
//                var convertedValue = _typeDetector.DetectAndConvertFromString(value);
//                record[header] = convertedValue;
//            }

//            return record;
//        }

//        #endregion
//    }

//    /// <summary>
//    /// CSV Writer utility for efficient CSV generation
//    /// </summary>
//    public class CsvWriter
//    {
//        public void WriteHeaders(StreamWriter writer, HashSet<string> headers)
//        {
//            var headerList = headers.OrderBy(h => h).ToList();
//            WriteRow(writer, headerList.Cast<object>().ToList());
//        }

//        public void WriteDataRow(StreamWriter writer, Dictionary<string, object>? record, HashSet<string> allHeaders, CsvImportOptions options)
//        {
//            var row = new List<object>();

//            foreach (var header in allHeaders.OrderBy(h => h))
//            {
//                if (record != null && record.ContainsKey(header))
//                {
//                    row.Add(record[header]);
//                }
//                else
//                {
//                    row.Add(options.PreserveNullValues ? null! : "");
//                }
//            }

//            WriteRow(writer, row, options);
//        }

//        private void WriteRow(StreamWriter writer, List<object> values, CsvImportOptions? options = null)
//        {
//            options ??= new CsvImportOptions();
//            var csvLine = new StringBuilder();

//            for (int i = 0; i < values.Count; i++)
//            {
//                if (i > 0)
//                {
//                    csvLine.Append(options.Delimiter);
//                }

//                var value = values[i];
//                var stringValue = ConvertValueToString(value, options);

//                // Check if value needs quoting
//                if (NeedsQuoting(stringValue, options))
//                {
//                    csvLine.Append(options.QuoteChar);
//                    // Escape any quote characters in the value
//                    csvLine.Append(stringValue.Replace(options.QuoteChar.ToString(), $"{options.QuoteChar}{options.QuoteChar}"));
//                    csvLine.Append(options.QuoteChar);
//                }
//                else
//                {
//                    csvLine.Append(stringValue);
//                }
//            }

//            writer.WriteLine(csvLine.ToString());
//        }

//        private string ConvertValueToString(object? value, CsvImportOptions options)
//        {
//            if (value == null)
//            {
//                return options.NullValueRepresentation;
//            }

//            return value switch
//            {
//                DateTime dt => dt.ToString(options.DateFormat),
//                decimal d => d.ToString(CultureInfo.InvariantCulture),
//                double d => d.ToString(CultureInfo.InvariantCulture),
//                float f => f.ToString(CultureInfo.InvariantCulture),
//                bool b => b.ToString().ToLower(),
//                _ => value.ToString() ?? ""
//            };
//        }

//        private bool NeedsQuoting(string value, CsvImportOptions options)
//        {
//            if (string.IsNullOrEmpty(value))
//            {
//                return false;
//            }

//            return value.Contains(options.Delimiter) ||
//                   value.Contains(options.QuoteChar) ||
//                   value.Contains('\n') ||
//                   value.Contains('\r') ||
//                   options.AlwaysQuoteStrings && !IsNumeric(value);
//        }

//        private bool IsNumeric(string value)
//        {
//            return double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out _);
//        }
//    }

//    /// <summary>
//    /// Enhanced Type Detector for CSV conversion
//    /// </summary>
//    public class TypeDetector
//    {
//        private readonly CsvImportOptions _options;
//        private readonly Dictionary<string, Type> _typeCache;

//        public TypeDetector(CsvImportOptions options)
//        {
//            _options = options ?? throw new ArgumentNullException(nameof(options));
//            _typeCache = new Dictionary<string, Type>();
//        }

//        public object DetectAndConvertFromString(string? value)
//        {
//            if (string.IsNullOrWhiteSpace(value))
//            {
//                return _options.PreserveNullValues ? null! : "";
//            }

//            // Check cache first for performance
//            if (_options.EnableTypeCaching && _typeCache.TryGetValue(value, out var cachedType))
//            {
//                return ConvertCachedType(value, cachedType);
//            }

//            // Try to parse special types if configured
//            if (_options.AutoDetectTypes)
//            {
//                var converted = TryParseSpecialTypes(value);
//                if (converted != null)
//                {
//                    if (_options.EnableTypeCaching)
//                    {
//                        _typeCache[value] = converted.GetType();
//                    }
//                    return converted;
//                }
//            }

//            return value;
//        }

//        private object? TryParseSpecialTypes(string text)
//        {
//            // Boolean
//            if (_options.ParseBooleans && (text.Equals("true", StringComparison.OrdinalIgnoreCase) ||
//                text.Equals("false", StringComparison.OrdinalIgnoreCase)))
//            {
//                return bool.Parse(text);
//            }

//            // Integer
//            if (_options.ParseNumbers && int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var intValue))
//            {
//                return intValue;
//            }

//            // Long
//            if (_options.ParseNumbers && long.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var longValue))
//            {
//                return longValue;
//            }

//            // Decimal
//            if (_options.ParseNumbers && decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out var decimalValue))
//            {
//                return _options.UseDecimalForNumbers ? decimalValue : (double)decimalValue;
//            }

//            // DateTime
//            if (_options.ParseDates && DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dateValue))
//            {
//                return _options.ConvertDatesToStrings ? text : dateValue;
//            }

//            // GUID
//            if (_options.ParseGuids && Guid.TryParse(text, out var guidValue))
//            {
//                return _options.PreserveGuidsAsStrings ? text : guidValue;
//            }

//            return null;
//        }

//        private object ConvertCachedType(string text, Type type)
//        {
//            try
//            {
//                return type.Name switch
//                {
//                    "Boolean" => bool.Parse(text),
//                    "Int32" => int.Parse(text, CultureInfo.InvariantCulture),
//                    "Int64" => long.Parse(text, CultureInfo.InvariantCulture),
//                    "Decimal" => decimal.Parse(text, CultureInfo.InvariantCulture),
//                    "Double" => double.Parse(text, CultureInfo.InvariantCulture),
//                    "DateTime" => DateTime.Parse(text, CultureInfo.InvariantCulture),
//                    "Guid" => Guid.Parse(text),
//                    _ => text
//                };
//            }
//            catch
//            {
//                // Conversion failed, remove from cache
//                if (_options.EnableTypeCaching)
//                {
//                    _typeCache.Remove(text);
//                }
//                return text;
//            }
//        }
//    }

//    /// <summary>
//    /// Configuration options for CSV import/export
//    /// </summary>
//    public class CsvImportOptions
//    {
//        // General Options
//        public char Delimiter { get; set; } = ',';
//        public char QuoteChar { get; set; } = '"';
//        public char EscapeChar { get; set; } = '\\';
//        public bool SkipEmptyRows { get; set; } = true;
//        public bool SkipNullValues { get; set; } = false;
//        public bool IgnoreParsingErrors { get; set; } = false;
//        public string NullValueRepresentation { get; set; } = "";

//        // Type Detection Options
//        public bool AutoDetectTypes { get; set; } = true;
//        public bool ParseNumbers { get; set; } = true;
//        public bool ParseBooleans { get; set; } = true;
//        public bool ParseDates { get; set; } = true;
//        public bool ParseGuids { get; set; } = true;
//        public bool EnableTypeCaching { get; set; } = true;

//        // Number Handling
//        public bool UseDecimalForNumbers { get; set; } = true;

//        // Date Handling
//        public bool ConvertDatesToStrings { get; set; } = false;
//        public string DateFormat { get; set; } = "yyyy-MM-dd HH:mm:ss";

//        // Value Preservation
//        public bool PreserveNullValues { get; set; } = true;
//        public bool PreserveGuidsAsStrings { get; set; } = true;

//        // CSV Writing Options
//        public bool AlwaysQuoteStrings { get; set; } = false;

//        // JSON Options
//        public Formatting JsonFormatting { get; set; } = Formatting.Indented;
//    }

//    /// <summary>
//    /// Extension methods for easier usage
//    /// </summary>
//    public static class CsvConverterExtensions
//    {
//        /// <summary>
//        /// Converts a list of objects to CSV bytes
//        /// </summary>
//        public static async Task<byte[]> ToCsvBytesAsync<T>(this List<T>? objects, CsvImportOptions? options = null)
//        {
//            if (objects == null)
//            {
//                throw new ArgumentNullException(nameof(objects));
//            }

//            var converter = new CsvExporterService(options);
//            var jsonData = objects.Select(obj =>
//            {
//                if (obj == null) return new Dictionary<string, object>();

//                var json = JsonConvert.SerializeObject(obj);
//                return JsonConvert.DeserializeObject<Dictionary<string, object>>(json) ?? new Dictionary<string, object>();
//            }).ToList();

//            return await converter.ConvertJsonToCsvAsync(jsonData);
//        }

//        /// <summary>
//        /// Converts CSV bytes to a list of dynamic objects
//        /// </summary>
//        public static async Task<List<Dictionary<string, object>>> FromCsvBytesAsync(this byte[]? csvBytes, CsvImportOptions? options = null)
//        {
//            if (csvBytes == null)
//            {
//                throw new ArgumentNullException(nameof(csvBytes));
//            }

//            var converter = new CsvExporterService(options);
//            return await converter.ConvertCsvToJsonAsync(csvBytes);
//        }
//    }

//    /// <summary>
//    /// Utility methods for common CSV operations
//    /// </summary>
//    public static class CsvUtilities
//    {
//        /// <summary>
//        /// Creates a memory stream from CSV bytes for HTTP responses
//        /// </summary>
//        public static MemoryStream CreateCsvStream(byte[] csvBytes)
//        {
//            if (csvBytes == null)
//            {
//                throw new ArgumentNullException(nameof(csvBytes));
//            }

//            return new MemoryStream(csvBytes);
//        }

//        /// <summary>
//        /// Gets CSV content type for HTTP responses
//        /// </summary>
//        public static string GetCsvContentType()
//        {
//            return "text/csv";
//        }

//        /// <summary>
//        /// Creates CSV download headers for HTTP responses
//        /// </summary>
//        public static Dictionary<string, string> CreateCsvDownloadHeaders(string filename = "data.csv")
//        {
//            return new Dictionary<string, string>
//            {
//                ["Content-Type"] = "text/csv; charset=utf-8",
//                ["Content-Disposition"] = $"attachment; filename=\"{filename}\"",
//                ["Cache-Control"] = "no-cache, no-store, must-revalidate",
//                ["Pragma"] = "no-cache",
//                ["Expires"] = "0"
//            };
//        }
//    }
//}