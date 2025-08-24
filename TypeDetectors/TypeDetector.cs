using CsvExporterLibrary.Interfaces;
using CsvExporterLibrary.Models;

namespace CsvExporterLibrary.TypeDetectors
{
    /// <summary>
    /// Service responsible for detecting and converting data types
    /// </summary>
    public class TypeDetector : ITypeDetector
    {
        private readonly Dictionary<string, Type> _typeCache;
        private readonly Dictionary<string, object> _valueCache;

        public TypeDetector()
        {
            _typeCache = new Dictionary<string, Type>();
            _valueCache = new Dictionary<string, object>();
        }

        public object DetectAndConvert(string? value, CsvImportOptions options)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return options.PreserveNullValues ? null! : "";
            }

            // Check value cache first for performance
            if (options.EnableTypeCaching && _valueCache.TryGetValue(value, out var cachedValue))
            {
                return cachedValue;
            }

            object result = value;

            if (options.AutoDetectTypes)
            {
                result = TryParseToSpecificType(value, options) ?? value;
            }

            // Cache the result if caching is enabled
            if (options.EnableTypeCaching)
            {
                _valueCache[value] = result;
            }

            return result;
        }

        public Type DetectType(string value, CsvImportOptions options)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return typeof(string);
            }

            // Check cache first
            if (options.EnableTypeCaching && _typeCache.TryGetValue(value, out var cachedType))
            {
                return cachedType;
            }

            var detectedType = GetDetectedType(value, options);

            // Cache the result
            if (options.EnableTypeCaching)
            {
                _typeCache[value] = detectedType;
            }

            return detectedType;
        }

        public bool TryParseValue<T>(string value, out T result)
        {
            result = default!;

            try
            {
                if (typeof(T) == typeof(int))
                {
                    if (int.TryParse(value, out var intResult))
                    {
                        result = (T)(object)intResult;
                        return true;
                    }
                }
                else if (typeof(T) == typeof(bool))
                {
                    if (bool.TryParse(value, out var boolResult))
                    {
                        result = (T)(object)boolResult;
                        return true;
                    }
                }
                else if (typeof(T) == typeof(DateTime))
                {
                    if (DateTime.TryParse(value, out var dateResult))
                    {
                        result = (T)(object)dateResult;
                        return true;
                    }
                }
                else if (typeof(T) == typeof(Guid))
                {
                    if (Guid.TryParse(value, out var guidResult))
                    {
                        result = (T)(object)guidResult;
                        return true;
                    }
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        private object? TryParseToSpecificType(string value, CsvImportOptions options)
        {
            // Boolean
            if (options.ParseBooleans && TryParseBoolean(value, out var boolValue))
            {
                return boolValue;
            }

            // Integer
            if (options.ParseNumbers && int.TryParse(value, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out var intValue))
            {
                return intValue;
            }

            // Long
            if (options.ParseNumbers && long.TryParse(value, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out var longValue))
            {
                return longValue;
            }

            // Decimal/Double
            if (options.ParseNumbers && decimal.TryParse(value, System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out var decimalValue))
            {
                return options.UseDecimalForNumbers ? decimalValue : (double)decimalValue;
            }

            // DateTime with custom formats
            if (options.ParseDates && TryParseDateTime(value, options, out var dateValue))
            {
                return options.ConvertDatesToStrings ? value : dateValue;
            }

            // GUID
            if (options.ParseGuids && Guid.TryParse(value, out var guidValue))
            {
                return options.PreserveGuidsAsStrings ? value : guidValue;
            }

            return null;
        }

        private bool TryParseBoolean(string value, out bool result)
        {
            result = false;

            if (string.IsNullOrWhiteSpace(value))
                return false;

            var normalizedValue = value.Trim().ToLowerInvariant();

            return normalizedValue switch
            {
                "true" or "yes" or "1" or "on" or "y" => SetResult(out result, true),
                "false" or "no" or "0" or "off" or "n" => SetResult(out result, false),
                _ => false
            };

            static bool SetResult(out bool result, bool value)
            {
                result = value;
                return true;
            }
        }

        private bool TryParseDateTime(string value, CsvImportOptions options, out DateTime result)
        {
            result = default;

            // Try standard parsing first
            if (DateTime.TryParse(value, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out result))
            {
                return true;
            }

            // Try custom formats
            if (options.CustomDateFormats.Count > 0)
            {
                foreach (var format in options.CustomDateFormats)
                {
                    if (DateTime.TryParseExact(value, format, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out result))
                    {
                        return true;
                    }
                }
            }

            // Try the configured date format
            if (!string.IsNullOrEmpty(options.DateFormat))
            {
                if (DateTime.TryParseExact(value, options.DateFormat, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out result))
                {
                    return true;
                }
            }

            return false;
        }

        private Type GetDetectedType(string value, CsvImportOptions options)
        {
            if (options.ParseBooleans && TryParseBoolean(value, out _))
                return typeof(bool);

            if (options.ParseNumbers && int.TryParse(value, out _))
                return typeof(int);

            if (options.ParseNumbers && long.TryParse(value, out _))
                return typeof(long);

            if (options.ParseNumbers && decimal.TryParse(value, out _))
                return options.UseDecimalForNumbers ? typeof(decimal) : typeof(double);

            if (options.ParseDates && TryParseDateTime(value, options, out _))
                return typeof(DateTime);

            if (options.ParseGuids && Guid.TryParse(value, out _))
                return typeof(Guid);

            return typeof(string);
        }
    }
}
