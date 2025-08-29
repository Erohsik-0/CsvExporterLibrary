using CsvExporterLibrary.Interfaces;
using CsvExporterLibrary.Models;


namespace CsvExporterLibrary.Validators
{
    /// <summary>
    /// Service responsible for validating CSV data and operations
    /// </summary>
    public class CsvValidator : ICsvValidator
    {
        public async Task<bool> ValidateExportDataAsync(List<Dictionary<string, object>>? data, CsvExportOptions options)
        {
            return await Task.Run(() =>
            {
                var errors = GetValidationErrors(data, options);
                return errors.Count == 0;
            });
        }

        public async Task<bool> ValidateImportDataAsync(byte[]? csvData, CsvImportOptions options)
        {
            return await Task.Run(() =>
            {
                var errors = GetCsvValidationErrors(csvData, options);
                return errors.Count == 0;
            });
        }

        public List<string> GetValidationErrors(List<Dictionary<string, object>>? data, CsvExportOptions options)
        {
            var errors = new List<string>();

            if (data == null)
            {
                errors.Add("Export data cannot be null");
                return errors;
            }

            if (data.Count == 0)
            {
                errors.Add("No data found to export");
                return errors;
            }

            if (data.Count > options.MaxRecordCount)
            {
                errors.Add($"Data contains {data.Count} records, which exceeds the maximum allowed {options.MaxRecordCount}");
            }

            // Validate field lengths if configured
            if (options.ValidateFieldLengths)
            {
                ValidateFieldLengths(data, options, errors);
            }

            // Validate headers
            var headers = ExtractUniqueHeaders(data);
            if (headers.Count == 0)
            {
                errors.Add("No valid headers found in the data");
            }

            return errors;
        }

        public List<string> GetCsvValidationErrors(byte[]? csvData, CsvImportOptions options)
        {
            var errors = new List<string>();

            if (csvData == null || csvData.Length == 0)
            {
                errors.Add("CSV data cannot be null or empty");
                return errors;
            }

            try
            {
                using var stream = new MemoryStream(csvData);
                using var reader = new StreamReader(stream);

                var headerLine = reader.ReadLine();
                if (string.IsNullOrWhiteSpace(headerLine))
                {
                    errors.Add("CSV file appears to be empty or contains no headers");
                    return errors;
                }

                var headers = ParseHeaderLine(headerLine, options);
                if (headers.Count == 0)
                {
                    errors.Add("No valid headers found in CSV data");
                }

                // Count records
                int recordCount = 0;
                while (reader.ReadLine() != null)
                {
                    recordCount++;
                    if (recordCount > options.MaxRecordCount)
                    {
                        errors.Add($"CSV contains more than {options.MaxRecordCount} records");
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                errors.Add($"Error validating CSV data: {ex.Message}");
            }

            return errors;
        }

        private void ValidateFieldLengths(List<Dictionary<string, object>> data, CsvExportOptions options, List<string> errors)
        {
            foreach (var record in data)
            {
                if (record == null) continue;

                foreach (var kvp in record)
                {
                    var stringValue = kvp.Value?.ToString() ?? "";
                    if (stringValue.Length > options.MaxFieldLength)
                    {
                        errors.Add($"Field '{kvp.Key}' contains a value that exceeds the maximum length of {options.MaxFieldLength} characters");
                        return; // Stop after first violation
                    }
                }
            }
        }

        private HashSet<string> ExtractUniqueHeaders(List<Dictionary<string, object>> data)
        {
            var headers = new HashSet<string>();
            foreach (var record in data)
            {
                if (record != null)
                {
                    foreach (var key in record.Keys)
                    {
                        headers.Add(key);
                    }
                }
            }
            return headers;
        }

        private List<string> ParseHeaderLine(string headerLine, CsvImportOptions options)
        {
            var headers = new List<string>();
            var currentHeader = new System.Text.StringBuilder();
            bool inQuotes = false;

            for (int i = 0; i < headerLine.Length; i++)
            {
                char c = headerLine[i];

                if (c == options.QuoteChar)
                {
                    inQuotes = !inQuotes;
                }
                else if (c == options.Delimiter && !inQuotes)
                {
                    headers.Add(options.TrimWhitespace ? currentHeader.ToString().Trim() : currentHeader.ToString());
                    currentHeader.Clear();
                }
                else
                {
                    currentHeader.Append(c);
                }
            }

            headers.Add(options.TrimWhitespace ? currentHeader.ToString().Trim() : currentHeader.ToString());
            return headers.Where(h => !string.IsNullOrWhiteSpace(h)).ToList();
        }
    }
}
