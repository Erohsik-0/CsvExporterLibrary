using CsvExporterLibrary.Interfaces;
using CsvExporterLibrary.Models;


namespace CsvExporterLibrary.DataTransformers
{
    // <summary>
    /// Service responsible for transforming data between formats
    /// </summary>
    public class CsvDataTransformer : ICsvDataTransformer
    {
        private readonly ITypeDetector _typeDetector;

        public CsvDataTransformer(ITypeDetector typeDetector)
        {
            _typeDetector = typeDetector ?? throw new ArgumentNullException(nameof(typeDetector));
        }

        public List<Dictionary<string, object>> TransformForExport(List<Dictionary<string, object>> data, CsvExportOptions options)
        {
            if (data == null || data.Count == 0)
            {
                return new List<Dictionary<string, object>>();
            }

            var transformedData = new List<Dictionary<string, object>>();

            foreach (var record in data)
            {
                if (record == null) continue;

                var transformedRecord = new Dictionary<string, object>();

                foreach (var kvp in record)
                {
                    var key = ApplyHeaderMapping(kvp.Key, options);
                    var value = TransformValueForExport(kvp.Value, options);

                    if (!options.SkipNullValues || value != null)
                    {
                        transformedRecord[key] = value;
                    }
                }

                if (transformedRecord.Count > 0)
                {
                    transformedData.Add(transformedRecord);
                }
            }

            return transformedData;
        }

        public List<Dictionary<string, object>> TransformFromImport(List<Dictionary<string, object>> data, CsvImportOptions options)
        {
            if (data == null || data.Count == 0)
            {
                return new List<Dictionary<string, object>>();
            }

            var transformedData = new List<Dictionary<string, object>>();

            foreach (var record in data)
            {
                var transformedRecord = TransformRecord(record, options);
                if (transformedRecord.Count > 0)
                {
                    transformedData.Add(transformedRecord);
                }
            }

            return transformedData;
        }

        public List<string> ExtractHeaders(List<Dictionary<string, object>> data, CsvExportOptions options)
        {
            var headers = new HashSet<string>();

            foreach (var record in data)
            {
                if (record != null)
                {
                    foreach (var key in record.Keys)
                    {
                        if (!options.SkipNullValues || record[key] != null)
                        {
                            var mappedKey = ApplyHeaderMapping(key, options);
                            headers.Add(mappedKey);
                        }
                    }
                }
            }

            return OrderHeaders(headers.ToList(), options);
        }

        public Dictionary<string, object> TransformRecord(Dictionary<string, object> record, CsvImportOptions options)
        {
            if (record == null) return new Dictionary<string, object>();

            var transformedRecord = new Dictionary<string, object>();

            foreach (var kvp in record)
            {
                var value = TransformValueFromImport(kvp.Value, options);

                if (!options.SkipNullValues || value != null)
                {
                    transformedRecord[kvp.Key] = value;
                }
            }

            return transformedRecord;
        }

        private string ApplyHeaderMapping(string originalHeader, CsvExportOptions options)
        {
            if (options.HeaderMappings != null && options.HeaderMappings.ContainsKey(originalHeader))
            {
                return options.HeaderMappings[originalHeader];
            }

            return originalHeader;
        }

        private List<string> OrderHeaders(List<string> headers, CsvExportOptions options)
        {
            if (options.CustomHeaderOrder != null && options.CustomHeaderOrder.Count > 0)
            {
                var orderedHeaders = new List<string>();

                // Add headers in custom order first
                foreach (var customHeader in options.CustomHeaderOrder)
                {
                    if (headers.Contains(customHeader))
                    {
                        orderedHeaders.Add(customHeader);
                    }
                }

                // Add remaining headers
                var remainingHeaders = headers.Except(orderedHeaders).ToList();
                if (options.SortHeadersAlphabetically)
                {
                    remainingHeaders.Sort();
                }

                orderedHeaders.AddRange(remainingHeaders);
                return orderedHeaders;
            }

            if (options.SortHeadersAlphabetically)
            {
                return headers.OrderBy(h => h).ToList();
            }

            return headers;
        }

        private object TransformValueForExport(object? value, CsvExportOptions options)
        {
            if (value == null)
            {
                return options.PreserveNullValues ? null! : options.NullValueRepresentation;
            }

            return value switch
            {
                DateTime dt => options.ConvertDatesToStrings ? dt.ToString(options.DateFormat) : dt,
                Guid guid => options.PreserveGuidsAsStrings ? guid.ToString() : guid,
                decimal d => options.UseDecimalForNumbers ? d : (double)d,
                float f => options.UseDecimalForNumbers ? (decimal)f : (double)f,
                bool b => b.ToString().ToLowerInvariant(),
                _ => value
            };
        }

        private object TransformValueFromImport(object? value, CsvImportOptions options)
        {
            if (value == null)
            {
                return options.PreserveNullValues ? null! : "";
            }

            if (value is string stringValue)
            {
                if (string.IsNullOrWhiteSpace(stringValue))
                {
                    return options.PreserveNullValues ? null! : "";
                }

                return _typeDetector.DetectAndConvert(stringValue, options);
            }

            return value;
        }
    }
}
