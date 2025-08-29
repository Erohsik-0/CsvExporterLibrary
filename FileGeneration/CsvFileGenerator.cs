using CsvExporterLibrary.Exceptions;
using CsvExporterLibrary.Interfaces;
using CsvExporterLibrary.Models;


namespace CsvExporterLibrary.FileGeneration
{
    /// <summary>
    /// Service responsible for generating CSV files
    /// </summary>
    public class CsvFileGenerator : ICsvFileGenerator
    {
        private readonly ICsvDataTransformer _transformer;
        private readonly ICsvValidator _validator;

        public CsvFileGenerator(ICsvDataTransformer transformer, ICsvValidator validator)
        {
            _transformer = transformer ?? throw new ArgumentNullException(nameof(transformer));
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        }

        public async Task<byte[]> GenerateCsvAsync(List<Dictionary<string, object>> data, CsvExportOptions options)
        {
            return await Task.Run(() => GenerateCsv(data, options));
        }

        public byte[] GenerateCsv(List<Dictionary<string, object>> data, CsvExportOptions options)
        {
            if (data == null || data.Count == 0)
            {
                throw new CsvExportException(
                    "No data provided for CSV generation",
                    "No data available to export",
                    "FileGeneration");
            }

            try
            {
                using var memoryStream = new MemoryStream();
                using var writer = new StreamWriter(memoryStream, System.Text.Encoding.UTF8);

                var transformedData = _transformer.TransformForExport(data, options);
                var headers = _transformer.ExtractHeaders(transformedData, options);

                // Write headers if enabled
                if (options.IncludeHeaders)
                {
                    WriteHeaders(writer, headers, options);
                }

                // Write data rows
                foreach (var record in transformedData)
                {
                    WriteDataRow(writer, record, headers, options);
                }

                writer.Flush();
                return memoryStream.ToArray();
            }
            catch (Exception ex) when (!(ex is CsvExportException))
            {
                throw new CsvExportException(
                    $"Failed to generate CSV: {ex.Message}",
                    "Unable to generate the CSV file",
                    ex,
                    "FileGeneration");
            }
        }

        public async Task<CsvExportResult> GenerateExportResultAsync(List<Dictionary<string, object>> data, CsvExportOptions options, string? fileName = null)
        {
            await _validator.ValidateExportDataAsync(data, options);

            var csvBytes = await GenerateCsvAsync(data, options);
            var headers = _transformer.ExtractHeaders(data, options);

            return new CsvExportResult
            {
                Data = csvBytes,
                FileName = fileName ?? "export.csv",
                ContentType = "text/csv; charset=utf-8",
                RecordCount = data?.Count ?? 0,
                Headers = headers,
                GeneratedAt = DateTime.UtcNow
            };
        }

        private void WriteHeaders(StreamWriter writer, List<string> headers, CsvExportOptions options)
        {
            WriteRow(writer, headers.Cast<object>().ToList(), options);
        }

        private void WriteDataRow(StreamWriter writer, Dictionary<string, object> record, List<string> headers, CsvExportOptions options)
        {
            var row = new List<object>();

            foreach (var header in headers)
            {
                if (record.ContainsKey(header))
                {
                    row.Add(record[header] ?? (options.PreserveNullValues ? null! : options.NullValueRepresentation));
                }
                else
                {
                    row.Add(options.PreserveNullValues ? null! : options.NullValueRepresentation);
                }
            }

            WriteRow(writer, row, options);
        }

        private void WriteRow(StreamWriter writer, List<object> values, CsvExportOptions options)
        {
            var csvLine = new System.Text.StringBuilder();

            for (int i = 0; i < values.Count; i++)
            {
                if (i > 0)
                {
                    csvLine.Append(options.Delimiter);
                }

                var value = values[i];
                var stringValue = ConvertValueToString(value, options);

                if (NeedsQuoting(stringValue, options))
                {
                    csvLine.Append(options.QuoteChar);
                    csvLine.Append(stringValue.Replace(options.QuoteChar.ToString(), $"{options.QuoteChar}{options.QuoteChar}"));
                    csvLine.Append(options.QuoteChar);
                }
                else
                {
                    csvLine.Append(stringValue);
                }
            }

            writer.Write(csvLine.ToString());
            writer.Write(options.LineEnding);
        }

        private string ConvertValueToString(object? value, CsvExportOptions options)
        {
            if (value == null)
            {
                return options.NullValueRepresentation;
            }

            return value switch
            {
                DateTime dt => dt.ToString(options.DateFormat),
                decimal d => d.ToString(options.NumberFormat, System.Globalization.CultureInfo.InvariantCulture),
                double d => d.ToString(options.NumberFormat, System.Globalization.CultureInfo.InvariantCulture),
                float f => f.ToString(options.NumberFormat, System.Globalization.CultureInfo.InvariantCulture),
                bool b => b.ToString().ToLowerInvariant(),
                _ => value.ToString() ?? ""
            };
        }

        private bool NeedsQuoting(string value, CsvExportOptions options)
        {
            if (string.IsNullOrEmpty(value))
            {
                return false;
            }

            if (options.QuoteAllValues)
            {
                return true;
            }

            return value.Contains(options.Delimiter) ||
                   value.Contains(options.QuoteChar) ||
                   value.Contains('\n') ||
                   value.Contains('\r') ||
                   (options.AlwaysQuoteStrings && !IsNumeric(value));
        }

        private bool IsNumeric(string value)
        {
            return double.TryParse(value, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out _);
        }
    }
}
